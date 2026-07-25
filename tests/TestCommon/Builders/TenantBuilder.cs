using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace TestCommon.Builders;

/// <summary>Fluent builder for the authoritative Tenant aggregate (Tenant service).</summary>
public sealed class TenantBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Acme Inc.";
    private string _slug = "acme-inc";
    private DateTime _createdAtUtc = TestClock.UtcNow;

    public TenantBuilder WithId(Guid id) { _id = id; return this; }
    public TenantBuilder WithName(string name) { _name = name; return this; }
    public TenantBuilder WithSlug(string slug) { _slug = slug; return this; }
    public TenantBuilder WithCreatedAt(DateTime createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }

    public DomainTenant Build() => new(_id, _name, _slug, _createdAtUtc);

    public static DomainTenant Default() => new TenantBuilder().Build();
}
