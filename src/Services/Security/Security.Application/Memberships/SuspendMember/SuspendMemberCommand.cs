using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Memberships.SuspendMember;

/// <summary>Suspends (<c>true</c>) or reactivates (<c>false</c>) a user's membership in a tenant.</summary>
public sealed record SuspendMemberCommand(
    Guid TenantId,
    Guid CallerUserId,
    Guid TargetUserId,
    bool Suspend
) : IRequest<Result>;
