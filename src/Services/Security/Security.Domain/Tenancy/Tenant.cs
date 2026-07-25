using BuildingBlocks.Domain;
using Security.Domain.Common;

namespace Security.Domain.Tenancy;

/// <summary>
/// Local read model of a tenant, kept in sync with the authoritative Tenant service through integration
/// events. It is used to validate a tenant (exists/active) when enrolling users via memberships. The
/// well-known public tenant is the default a self-registered user is enrolled into.
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
