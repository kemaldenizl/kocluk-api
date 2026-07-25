namespace Security.Application.Common.Tenancy;

/// <summary>
/// Platform-wide tenant defaults. The public tenant is the tenant a freshly self-registered user is
/// automatically enrolled into, and is also used as the originating tenant for auth-related integration
/// events (email verification, password reset, ...). It matches the default tenant seeded on startup.
/// </summary>
public sealed class TenantDefaultsOptions
{
    public const string SectionName = "Tenancy";

    public static readonly Guid PublicTenantIdValue = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid PublicTenantId { get; init; } = PublicTenantIdValue;
}
