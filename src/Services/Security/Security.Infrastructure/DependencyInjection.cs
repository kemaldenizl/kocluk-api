using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Authorization.Validation;
using Security.Application.Abstractions.Authentication;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Persistence.Repositories;
using Security.Infrastructure.Persistence.Seed;
using Security.Infrastructure.Security;
using Security.Infrastructure.Security.Jwt;
using Security.Infrastructure.Security.Redis;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.RequestContext;
using Security.Infrastructure.Auditing;
using Security.Infrastructure.RequestContext;
using BuildingBlocks.Messaging.RabbitMq;
using MassTransit;
using Security.Infrastructure.Messaging.Consumers;
using Security.Application.Abstractions.Memberships;
using Security.Application.Common.Tenancy;
using Security.Infrastructure.Memberships;

namespace Security.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' was not found.");

        services.Configure<TenantDefaultsOptions>(configuration.GetSection(TenantDefaultsOptions.SectionName));

        services.AddDbContext<SecurityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "security");
            });

            options.UseOpenIddict();
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));
        services.Configure<RedisRevocationOptions>(configuration.GetSection(RedisRevocationOptions.SectionName));

        // Shared JWT validation + permission-based authorization (declared once in a building block).
        // The Security service additionally plugs in Redis-backed revocation via IAccessTokenValidator;
        // other services simply omit that registration and stay dependency-light.
        services.AddJwtAuthentication(configuration);
        services.AddPermissionAuthorization();
        services.AddScoped<IAccessTokenValidator, RedisAccessTokenValidator>();
        services.AddCurrentUser();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IUserPlatformRoleRepository, UserPlatformRoleRepository>();
        services.AddScoped<IMembershipQueryService, MembershipQueryService>();
        services.AddScoped<ITenantAccessAuthorizer, TenantAccessAuthorizer>();
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

        // Shared RabbitMQ/MassTransit plumbing (outbox, host, IEventPublisher) lives in a building block.
        // This service stays independent: it owns its DbContext and registers only its own consumers.
        services.AddRabbitMqMessaging<SecurityDbContext>(configuration, x =>
        {
            // Tenant lifecycle is owned by the Tenant microservice; we consume its events to keep a
            // local tenant read model in sync (used to validate tenants on incoming requests).
            x.AddConsumer<TenantCreatedConsumer>();
            x.AddConsumer<TenantUpdatedConsumer>();
            x.AddConsumer<TenantDeletedConsumer>();
        });

        return services;
    }
}
