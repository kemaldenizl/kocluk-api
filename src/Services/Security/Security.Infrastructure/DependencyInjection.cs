using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Abstractions.Authentication;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Security;
using Security.Domain.Authorization;
using Security.Infrastructure.Authorization;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Persistence.Repositories;
using Security.Infrastructure.Persistence.Seed;
using Security.Infrastructure.Security;
using Security.Infrastructure.Security.Jwt;
using Security.Infrastructure.Security.Redis;
using Microsoft.AspNetCore.Http;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.RequestContext;
using Security.Infrastructure.Auditing;
using Security.Infrastructure.RequestContext;
using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Security.Application.Abstractions.Messaging;
using Security.Infrastructure.Messaging;
using Security.Infrastructure.Messaging.Consumers;
using Security.Application.Abstractions.Tenancy;
using Security.Infrastructure.Tenancy;

namespace Security.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.Configure<MultiTenancyOptions>(configuration.GetSection(MultiTenancyOptions.SectionName));
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddDbContext<SecurityDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "security");
            });

            options.UseOpenIddict();

            // Tenant isolation: stamp TenantId on inserts and block cross-tenant writes.
            options.AddInterceptors(serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>());

            // Both ends of tenant-scoped required relationships share the same tenant filter,
            // so the required-navigation/query-filter interaction is intentional and safe.
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.Configure<RedisRevocationOptions>(configuration.GetSection(RedisRevocationOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt signing key must be at least 32 characters.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = CustomClaimTypes.Email,
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {

                        var revocationStore = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenRevocationStore>();

                        var principal = context.Principal;
                        if (principal is null)
                        {
                            context.Fail("Token principal is missing.");
                            return;
                        }

                        var jti = principal.FindFirst(CustomClaimTypes.JwtId)?.Value;
                        if (string.IsNullOrWhiteSpace(jti))
                        {
                            context.Fail("Token does not contain a valid jti.");
                            return;
                        }

                        var sub = principal.FindFirst(CustomClaimTypes.Subject)?.Value;
                        if (!Guid.TryParse(sub, out var userId))
                        {
                            context.Fail("Token does not contain a valid subject.");
                            return;
                        }

                        var multiTenancyOptions = context.HttpContext.RequestServices
                            .GetRequiredService<IOptions<MultiTenancyOptions>>().Value;

                        if (multiTenancyOptions.Enabled &&
                            !Guid.TryParse(principal.FindFirst(CustomClaimTypes.TenantId)?.Value, out _))
                        {
                            context.Fail("Token does not contain a valid tenant.");
                            return;
                        }

                        var iatValue = principal.FindFirst(CustomClaimTypes.IssuedAt)?.Value;
                        if (!long.TryParse(iatValue, out var issuedAtUnix))
                        {
                            context.Fail("Token does not contain a valid iat.");
                            return;
                        }

                        var issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix).UtcDateTime;

                        var isJtiRevoked = await revocationStore.IsRevokedAsync(
                            jti,
                            context.HttpContext.RequestAborted);

                        if (isJtiRevoked)
                        {
                            context.Fail("Token has been revoked.");
                            return;
                        }

                        var isUserInvalidated = await revocationStore.IsUserTokenInvalidatedAsync(
                            userId,
                            issuedAtUtc,
                            context.HttpContext.RequestAborted);

                        if (isUserInvalidated)
                        {
                            context.Fail("User tokens have been invalidated.");
                            return;
                        }

                        var sid = principal.FindFirst(CustomClaimTypes.SessionId)?.Value;
                        if (Guid.TryParse(sid, out var sessionId))
                        {
                            var isSessionInvalidated = await revocationStore.IsSessionTokenInvalidatedAsync(
                                sessionId,
                                issuedAtUtc,
                                context.HttpContext.RequestAborted);

                            if (isSessionInvalidated)
                            {
                                context.Fail("Session tokens have been invalidated.");
                            }
                        }
                    },
                    OnChallenge = async context =>
                    {
                        if (context.Response.HasStarted)
                            return;

                        context.HandleResponse();

                        var problem = new ProblemDetails
                        {
                            Type = "https://httpstatuses.com/401",
                            Title = "Unauthorized",
                            Status = StatusCodes.Status401Unauthorized,
                            Detail = "Authentication is required or the access token is invalid.",
                            Instance = context.HttpContext.Request.Path
                        };

                        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";

                        await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
                    },
                    OnForbidden = async context =>
                    {
                        var problem = new ProblemDetails
                        {
                            Type = "https://httpstatuses.com/403",
                            Title = "Forbidden",
                            Status = StatusCodes.Status403Forbidden,
                            Detail = "You do not have permission to access this resource.",
                            Instance = context.HttpContext.Request.Path
                        };

                        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";

                        await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
                    }
                };
            }
        );

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PermissionCodes.UsersRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.UsersRead)));

            options.AddPolicy(PermissionCodes.UsersManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.UsersManage)));

            options.AddPolicy(PermissionCodes.RolesRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RolesRead)));

            options.AddPolicy(PermissionCodes.RolesManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RolesManage)));

            options.AddPolicy(PermissionCodes.PermissionsRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.PermissionsRead)));

            options.AddPolicy(PermissionCodes.PermissionsManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.PermissionsManage)));

            options.AddPolicy(PermissionCodes.SessionsRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SessionsRead)));

            options.AddPolicy(PermissionCodes.SessionsManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SessionsManage)));
        });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IAccessTokenRevocationStore, RedisAccessTokenRevocationStore>();

        services.AddSingleton<PasswordHasher>();
        services.AddScoped<IdentitySeeder>();

        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddHttpContextAccessor();
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.AddScoped<IAuditLogFactory, AuditLogFactory>();

        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddSingleton<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();

        services.AddScoped<IPasswordChangeTokenRepository, PasswordChangeTokenRepository>();
        services.AddSingleton<IPasswordChangeTokenGenerator, PasswordChangeTokenGenerator>();

        services.AddScoped<IEmailChangeTokenRepository, EmailChangeTokenRepository>();
        services.AddSingleton<IEmailChangeTokenGenerator, EmailChangeTokenGenerator>();

        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddSingleton<IEmailVerificationTokenGenerator, EmailVerificationTokenGenerator>();

        services.AddScoped<IMfaMethodRepository, MfaMethodRepository>();

        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<ITotpSecretProtector, TotpSecretProtector>();
        services.AddSingleton<IRecoveryCodeService, RecoveryCodeService>();
        services.AddSingleton<IMfaChallengeTokenService, MfaChallengeTokenService>();
        
        services.Configure<SecurityTokenInvalidationOptions>(configuration.GetSection(SecurityTokenInvalidationOptions.SectionName));

        services.AddDataProtection();

        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
                              ?? throw new InvalidOperationException("RabbitMq configuration section is missing.");

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<SecurityDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            // Tenant lifecycle is owned by the Tenant microservice; we consume its events to keep a
            // local tenant read model in sync (used to validate tenants on incoming requests).
            x.AddConsumer<TenantCreatedConsumer>();
            x.AddConsumer<TenantUpdatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, host =>
                {
                    host.Username(rabbitMqOptions.Username);
                    host.Password(rabbitMqOptions.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }
}