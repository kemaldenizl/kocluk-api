namespace Security.Domain.Memberships;

/// <summary>
/// Lifecycle state of a user's membership in a tenant. Removed memberships are kept (soft) so a
/// user's tenant history is preserved and a former member can be re-activated later.
/// </summary>
public enum MembershipStatus
{
    Active = 1,
    Suspended = 2,
    Removed = 3
}
