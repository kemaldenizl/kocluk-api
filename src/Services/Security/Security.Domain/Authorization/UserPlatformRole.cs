using Security.Domain.Common;

namespace Security.Domain.Authorization;

/// <summary>
/// Grants a <see cref="RoleScope.Platform"/> role directly to a user, independently of any tenant
/// membership. This is how the system administrator is modelled: a platform role that grants access to
/// (and management of) every tenant.
/// </summary>
public sealed class UserPlatformRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private UserPlatformRole()
    {
    }

    public UserPlatformRole(Guid userId, Guid roleId, DateTime assignedAtUtc)
    {
        UserId = Guard.AgainstEmpty(userId, nameof(userId));
        RoleId = Guard.AgainstEmpty(roleId, nameof(roleId));
        AssignedAtUtc = Guard.AgainstDefault(assignedAtUtc, nameof(assignedAtUtc));
    }
}
