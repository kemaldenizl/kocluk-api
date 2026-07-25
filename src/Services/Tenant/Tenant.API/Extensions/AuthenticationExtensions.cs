using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Tenant.API.Authorization;

namespace Tenant.API.Extensions;

/// <summary>
/// Wires up bearer authentication for the Tenant service using the same JWT (issuer/audience/signing key)
/// that the Security service issues, so a Security-issued access token authenticates here too. Tenant
/// lifecycle mutations require the platform system-administrator claim.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddTenantAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");
        var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

        if (signingKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.SystemAdmin, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c =>
                        c.Type == AuthorizationPolicies.SystemAdminClaim &&
                        string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase))));
        });

        return services;
    }
}
