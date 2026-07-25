using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace FunctionalTests;

/// <summary>
/// Hosts the real API gateway in-process. JWT settings are injected as in-memory configuration so the
/// factory is independent of the environment's appsettings, and the same signing key is reused to mint
/// tokens the gateway will accept — letting the edge-authentication behavior be tested end to end without
/// any downstream microservice running.
/// </summary>
public sealed class GatewayApplicationFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "test-issuer";
    public const string Audience = "test-audience";
    public const string SigningKey = "functional-tests-signing-key-at-least-32-chars-long";

    public GatewayApplicationFactory()
    {
        // The gateway's Program reads the "Jwt" section from builder.Configuration during startup — before
        // WebApplicationFactory's ConfigureAppConfiguration hooks merge in. Environment variables are read
        // by CreateBuilder at construction time, so they are the reliable seam for a minimal-hosting app.
        // "__" is the configuration hierarchy separator.
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Neutral environment so appsettings.Development.json (which carries its own Jwt section) is not
        // loaded; only base appsettings.json (routes/clusters) plus the Jwt env vars above apply, making
        // token validation deterministic against the key this factory signs with.
        builder.UseEnvironment("Testing");
    }

    /// <summary>Mints a bearer token the gateway will accept (valid signature/issuer/audience/lifetime).</summary>
    public string CreateAccessToken(DateTime? expiresAtUtc = null)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var expires = expiresAtUtc ?? DateTime.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", Guid.NewGuid().ToString())],
            notBefore: expires.AddMinutes(-15),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
