namespace Security.Application.Common.Errors;

public static class MembershipErrors
{
    public static readonly Error NotFound = new("membership.not_found", "Membership was not found.");
    public static readonly Error AlreadyActive = new("membership.already_active", "The user is already an active member of this tenant.");
    public static readonly Error RoleNotInTenant = new("membership.role_not_in_tenant", "One or more roles do not belong to the target tenant.");
}
