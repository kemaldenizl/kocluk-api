using BuildingBlocks.Domain;
using Tenant.Domain.Common;

namespace Tenant.Domain.Tenants;

/// <summary>
/// The Tenant aggregate is the authoritative record of a tenant on the platform. This service owns the
/// tenant lifecycle; other services (Security, and later Payment) keep their own read models in sync by
/// consuming the integration events raised when a tenant is created, changed or removed.
/// <para>
/// <see cref="Slug"/> is the stable, human-friendly identifier and is immutable once assigned — callers
/// resolve a tenant by slug, so changing it would break those references. <see cref="Name"/> and the
/// active flag are the mutable parts of a tenant's state.
/// </para>
/// </summary>
public sealed class Tenant : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

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
        UpdatedAtUtc = CreatedAtUtc;
    }

    /// <summary>Changes the display name. Returns <c>true</c> when the name actually changed.</summary>
    public bool Rename(string name, DateTime at)
    {
        var newName = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        if (string.Equals(Name, newName, StringComparison.Ordinal))
            return false;

        Name = newName;
        Touch(at);
        return true;
    }

    /// <summary>Enables the tenant. Returns <c>true</c> when the state actually changed.</summary>
    public bool Activate(DateTime at)
    {
        if (IsActive)
            return false;

        IsActive = true;
        Touch(at);
        return true;
    }

    /// <summary>Disables the tenant without deleting it. Returns <c>true</c> when the state actually changed.</summary>
    public bool Deactivate(DateTime at)
    {
        if (!IsActive)
            return false;

        IsActive = false;
        Touch(at);
        return true;
    }

    private void Touch(DateTime at)
    {
        UpdatedAtUtc = Guard.AgainstDefault(at, nameof(at));
    }
}
