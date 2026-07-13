using Security.Domain.Abstractions;
using Security.Domain.Common;

namespace Security.Domain.Tenancy;

/// <summary>
/// A tenant is the top-level isolation boundary of the security service. Every tenant-scoped
/// entity (see <see cref="ITenantScoped"/>) belongs to exactly one tenant. In single-tenant
/// deployments a single well-known default tenant is used.
/// </summary>
public sealed class Tenant : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Tenant()
    {
    }

    public Tenant(Guid id, string name, string slug, DateTime createdAtUtc)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Slug = Guard.AgainstNullOrWhiteSpace(slug, nameof(slug));
        CreatedAtUtc = Guard.AgainstDefault(createdAtUtc, nameof(createdAtUtc));

        IsActive = true;
    }

    public void Rename(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
