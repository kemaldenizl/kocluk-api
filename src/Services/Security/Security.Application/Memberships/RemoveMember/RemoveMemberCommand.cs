using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Memberships.RemoveMember;

/// <summary>Soft-removes a user's membership in a tenant (status set to Removed, kept for history).</summary>
public sealed record RemoveMemberCommand(
    Guid TenantId,
    Guid CallerUserId,
    Guid TargetUserId
) : IRequest<Result>;
