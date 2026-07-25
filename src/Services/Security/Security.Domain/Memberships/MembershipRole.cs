using Security.Domain.Common;

namespace Security.Domain.Memberships;

/// <summary>
/// A role granted to a membership. Roles are tenant-scoped (see <see cref="Security.Domain.Authorization.Role"/>),
/// so the roles attached to a membership always belong to the same tenant as the membership.
/// </summary>
public sealed class MembershipRole
{
    public Guid MembershipId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private MembershipRole()
    {
    }

    public MembershipRole(Guid membershipId, Guid roleId, DateTime assignedAtUtc)
    {
        MembershipId = Guard.AgainstEmpty(membershipId, nameof(membershipId));
        RoleId = Guard.AgainstEmpty(roleId, nameof(roleId));
        AssignedAtUtc = Guard.AgainstDefault(assignedAtUtc, nameof(assignedAtUtc));
    }
}
