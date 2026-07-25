namespace Tenant.Application.Common.Tenancy;

/// <summary>
/// Platform-wide tenant defaults owned by the Tenant service (the authority for the tenant lifecycle).
/// The public tenant is provisioned on startup and is the tenant self-registered users are enrolled into.
/// </summary>
public sealed class TenantDefaultsOptions
{
    public const string SectionName = "Tenancy";

    public static readonly Guid PublicTenantIdValue = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid PublicTenantId { get; init; } = PublicTenantIdValue;

    public string PublicTenantName { get; init; } = "Public";

    public string PublicTenantSlug { get; init; } = "public";
}
