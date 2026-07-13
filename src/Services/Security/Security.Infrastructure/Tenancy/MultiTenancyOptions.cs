namespace Security.Infrastructure.Tenancy;

/// <summary>
/// Controls whether the security service runs in multi-tenant or single-tenant mode.
/// A single configuration switch (<see cref="Enabled"/>) toggles the behaviour; the database
/// schema is identical in both modes, so a deployment can flip between them without a migration.
/// </summary>
public sealed class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>Well-known tenant used for every row in single-tenant mode and as the default/system tenant.</summary>
    public static readonly Guid DefaultTenantIdValue = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>When <c>true</c> the service isolates data per tenant and resolves a tenant per request.</summary>
    public bool Enabled { get; init; }

    /// <summary>The tenant every row falls under in single-tenant mode. Defaults to <see cref="DefaultTenantIdValue"/>.</summary>
    public Guid DefaultTenantId { get; init; } = DefaultTenantIdValue;

    /// <summary>Header carrying the tenant id/slug on anonymous requests (login, register, ...).</summary>
    public string HeaderName { get; init; } = "X-Tenant-Id";
}
