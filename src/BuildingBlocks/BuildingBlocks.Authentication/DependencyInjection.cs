using System.Text;
using BuildingBlocks.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Authentication;

/// <summary>
/// Cross-cutting authentication/authorization wiring shared by the API gateway and every
/// microservice, so JWT validation and permission policies are declared once instead of per service.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Validates the Security-issued bearer token (signature/issuer/audience/lifetime) from the
    /// given configuration section, emits RFC 7807 problem responses, and — when a service registers
    /// an <see cref="Authorization.Validation.IAccessTokenValidator"/> — invokes it after signature
    /// validation for revocation-style checks.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = JwtAuthenticationOptions.SectionName)
    {
        var options = configuration.GetSection(sectionName).Get<JwtAuthenticationOptions>()
            ?? throw new InvalidOperationException($"'{sectionName}' configuration section is missing.");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException($"'{sectionName}:Issuer' is missing.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException($"'{sectionName}:Audience' is missing.");

        if (string.IsNullOrWhiteSpace(options.SigningKey) || options.SigningKey.Length < 32)
            throw new InvalidOperationException($"'{sectionName}:SigningKey' must be at least 32 characters.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,

                    ValidateAudience = true,
                    ValidAudience = options.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = CustomClaimTypes.Email,
                    RoleClaimType = "role"
                };

                bearer.Events = ProblemDetailsJwtBearerEvents.Create();
            });

        return services;
    }

    /// <summary>
    /// Registers one authorization policy per permission code (policy name == permission code) plus
    /// the shared <see cref="PermissionAuthorizationHandler"/>. Pass a subset to expose only the
    /// permissions a given service actually enforces; defaults to the full <see cref="PermissionCatalog"/>.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(
        this IServiceCollection services,
        IEnumerable<string>? permissionCodes = null)
    {
        var codes = permissionCodes ?? PermissionCatalog.All;

        services.AddAuthorization(options =>
        {
            foreach (var code in codes)
            {
                options.AddPolicy(code, policy =>
                    policy.Requirements.Add(new PermissionRequirement(code)));
            }
        });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Edge authentication for the API gateway: validates the token and registers the
    /// <see cref="AuthorizationPolicyNames.Authenticated"/> policy (any authenticated caller). The
    /// gateway does not enforce permissions — that stays with the owning service (defense in depth).
    /// </summary>
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = JwtAuthenticationOptions.SectionName)
    {
        services.AddJwtAuthentication(configuration, sectionName);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicyNames.Authenticated, policy => policy.RequireAuthenticatedUser());

        return services;
    }

    /// <summary>Registers the ambient <see cref="ICurrentUser"/> accessor over the current request.</summary>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
