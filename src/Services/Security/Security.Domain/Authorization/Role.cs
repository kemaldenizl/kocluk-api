using BuildingBlocks.Domain;
using Security.Domain.Common;

namespace Security.Domain.Authorization;

/// <summary>
/// A named set of permissions. A role is either <see cref="RoleScope.Tenant"/>-scoped (belongs to a
/// single tenant and is granted via memberships) or <see cref="RoleScope.Platform"/>-scoped
/// (tenant-independent, granted directly to a user). Platform roles have no <see cref="TenantId"/>.
/// </summary>
public sealed class Role : AggregateRoot
{
    private readonly List<RolePermission> _permissions = [];

    public Guid Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public RoleScope Scope { get; private set; }
    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role()
    {
    }

    private Role(Guid id, Guid? tenantId, RoleScope scope, string name, string normalizedName)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        NormalizedName = Guard.AgainstNullOrWhiteSpace(normalizedName, nameof(normalizedName));
        Scope = scope;
        TenantId = tenantId;
    }

    /// <summary>Creates a tenant-scoped role that can be granted to memberships of the given tenant.</summary>
    public static Role ForTenant(Guid id, Guid tenantId, string name, string normalizedName)
    {
        Guard.AgainstEmpty(tenantId, nameof(tenantId));
        return new Role(id, tenantId, RoleScope.Tenant, name, normalizedName);
    }

    /// <summary>Creates a tenant-independent platform role granted directly to users.</summary>
    public static Role Platform(Guid id, string name, string normalizedName)
    {
        return new Role(id, null, RoleScope.Platform, name, normalizedName);
    }

    public void Rename(string name, string normalizedName)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        NormalizedName = Guard.AgainstNullOrWhiteSpace(normalizedName, nameof(normalizedName));
    }

    public void AddPermission(Guid permissionId, DateTime assignedAtUtc)
    {
        permissionId = Guard.AgainstEmpty(permissionId, nameof(permissionId));
        assignedAtUtc = Guard.AgainstDefault(assignedAtUtc, nameof(assignedAtUtc));

        if (_permissions.Any(x => x.PermissionId == permissionId))
            return;

        _permissions.Add(new RolePermission(Id, permissionId, assignedAtUtc));
    }

    public void RemovePermission(Guid permissionId)
    {
        permissionId = Guard.AgainstEmpty(permissionId, nameof(permissionId));

        var existing = _permissions.FirstOrDefault(x => x.PermissionId == permissionId);
        if (existing is null)
            return;

        _permissions.Remove(existing);
    }
}
