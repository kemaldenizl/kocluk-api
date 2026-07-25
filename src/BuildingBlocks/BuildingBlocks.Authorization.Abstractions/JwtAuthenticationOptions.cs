namespace BuildingBlocks.Authorization;

/// <summary>
/// Bearer-token validation settings bound from the shared <c>"Jwt"</c> configuration section. These
/// are the values every service needs to validate a Security-issued token (they must match the
/// Security service's issuing configuration).
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
}
