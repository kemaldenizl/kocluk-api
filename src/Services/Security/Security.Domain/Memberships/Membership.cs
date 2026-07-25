using BuildingBlocks.Domain;
using Security.Domain.Common;

namespace Security.Domain.Memberships;

/// <summary>
/// Links a <see cref="Security.Domain.Users.User"/> to a tenant. A user can hold many memberships (one per
/// tenant) and sees the data of every tenant they are an active member of. Each membership carries the
/// tenant-scoped roles the user has within that tenant and a <see cref="MembershipStatus"/> that also
/// captures history (removed memberships are retained rather than deleted).
/// </summary>
public sealed class Membership : AggregateRoot
{
    private readonly List<MembershipRole> _roles = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<MembershipRole> Roles => _roles.AsReadOnly();

    public bool IsActive => Status == MembershipStatus.Active;

    private Membership()
    {
    }

    public Membership(Guid id, Guid userId, Guid tenantId, DateTime joinedAtUtc)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        UserId = Guard.AgainstEmpty(userId, nameof(userId));
        TenantId = Guard.AgainstEmpty(tenantId, nameof(tenantId));
        JoinedAtUtc = Guard.AgainstDefault(joinedAtUtc, nameof(joinedAtUtc));

        Status = MembershipStatus.Active;
        UpdatedAtUtc = JoinedAtUtc;
    }

    public void AddRole(Guid roleId, DateTime assignedAtUtc)
    {
        roleId = Guard.AgainstEmpty(roleId, nameof(roleId));
        assignedAtUtc = Guard.AgainstDefault(assignedAtUtc, nameof(assignedAtUtc));

        if (_roles.Any(x => x.RoleId == roleId))
            return;

        _roles.Add(new MembershipRole(Id, roleId, assignedAtUtc));
        Touch(assignedAtUtc);
    }

    public void RemoveRole(Guid roleId, DateTime at)
    {
        roleId = Guard.AgainstEmpty(roleId, nameof(roleId));

        var existing = _roles.FirstOrDefault(x => x.RoleId == roleId);
        if (existing is null)
            return;

        _roles.Remove(existing);
        Touch(at);
    }

    /// <summary>Re-activates a suspended or previously removed membership.</summary>
    public void Reactivate(DateTime at)
    {
        if (Status == MembershipStatus.Active)
            return;

        Status = MembershipStatus.Active;
        Touch(at);
    }

    public void Suspend(DateTime at)
    {
        if (Status == MembershipStatus.Suspended)
            return;

        Status = MembershipStatus.Suspended;
        Touch(at);
    }

    /// <summary>Soft-removes the membership, keeping the row (and its roles) for historical record.</summary>
    public void Remove(DateTime at)
    {
        if (Status == MembershipStatus.Removed)
            return;

        Status = MembershipStatus.Removed;
        Touch(at);
    }

    private void Touch(DateTime at)
    {
        UpdatedAtUtc = Guard.AgainstDefault(at, nameof(at));
    }
}
