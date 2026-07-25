using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Memberships.AssignRoleToMember;

/// <summary>Grants a tenant role to a user's membership in that tenant.</summary>
public sealed record AssignRoleToMemberCommand(
    Guid TenantId,
    Guid CallerUserId,
    Guid TargetUserId,
    Guid RoleId
) : IRequest<Result>;
