using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Memberships.RemoveRoleFromMember;

/// <summary>Removes a tenant role from a user's membership in that tenant.</summary>
public sealed record RemoveRoleFromMemberCommand(
    Guid TenantId,
    Guid CallerUserId,
    Guid TargetUserId,
    Guid RoleId
) : IRequest<Result>;
