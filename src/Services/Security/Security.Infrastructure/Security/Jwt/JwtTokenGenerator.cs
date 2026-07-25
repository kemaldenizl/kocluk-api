using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Abstractions.Authentication;
using Security.Application.Abstractions.Memberships;
using Security.Application.Auth.Dtos;
using Security.Application.Common.Security;

namespace Security.Infrastructure.Security.Jwt;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : ITokenGenerator
{
    private static readonly JsonSerializerOptions MembershipSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly JwtOptions _options = options.Value;

    public Task<AccessTokenDto> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        UserAccessContext accessContext,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.Subject, userId.ToString()),
            new(CustomClaimTypes.Email, email),
            new(CustomClaimTypes.JwtId, Guid.NewGuid().ToString("N")),
            new(CustomClaimTypes.IssuedAt, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(CustomClaimTypes.SystemAdmin, accessContext.IsSystemAdmin ? "true" : "false", ClaimValueTypes.Boolean)
        };

        if (sessionId.HasValue)
        {
            claims.Add(new Claim(CustomClaimTypes.SessionId, sessionId.Value.ToString()));
        }

        // The user's tenant memberships are carried as a single JSON claim so downstream services can
        // scope data to the union of the tenants the user belongs to without any extra lookups.
        var membershipsPayload = accessContext.Memberships
            .Select(m => new MembershipClaim(m.TenantId, m.Roles, m.Status))
            .ToArray();

        claims.Add(new Claim(
            CustomClaimTypes.Memberships,
            JsonSerializer.Serialize(membershipsPayload, MembershipSerializerOptions),
            JsonClaimValueTypes.JsonArray));

        claims.AddRange(accessContext.PlatformRoles
            .Select(roleName => new Claim(CustomClaimTypes.PlatformRole, roleName)));

        claims.AddRange(accessContext.Permissions
            .Select(permission => new Claim(CustomClaimTypes.Permission, permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAtUtc,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        var accessToken = handler.WriteToken(token);

        return Task.FromResult(new AccessTokenDto(accessToken, expiresAtUtc));
    }

    private static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
            throw new InvalidOperationException("Jwt:Issuer is missing.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            throw new InvalidOperationException("Jwt:Audience is missing.");

        if (string.IsNullOrWhiteSpace(options.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey is missing.");

        if (options.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
    }

    private sealed record MembershipClaim(Guid TenantId, IReadOnlyList<string> Roles, string Status);
}
