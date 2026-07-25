using MediatR;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Memberships;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Authorization;

namespace Security.Application.Memberships.SuspendMember;

public sealed class SuspendMemberCommandHandler(
    ITenantAccessAuthorizer tenantAccessAuthorizer,
    IMembershipRepository membershipRepository,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<SuspendMemberCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(SuspendMemberCommand request, CancellationToken cancellationToken)
    {
        var authorized = await tenantAccessAuthorizer.HasTenantPermissionAsync(
            request.CallerUserId, request.TenantId, PermissionCodes.MembersManage, cancellationToken);
        if (!authorized)
            return Result.Failure(AccessErrors.Forbidden);

        var membership = await membershipRepository.GetByUserAndTenantAsync(
            request.TargetUserId, request.TenantId, cancellationToken);
        if (membership is null)
            return Result.Failure(MembershipErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;

        if (request.Suspend)
            membership.Suspend(utcNow);
        else
            membership.Reactivate(utcNow);

        await accessTokenRevocationStore.RevokeUserAsync(
            request.TargetUserId,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);

        await auditLogRepository.AddAsync(
            auditLogFactory.Create(
                request.Suspend ? AuditActionType.MembershipSuspended : AuditActionType.MembershipReactivated,
                AuditPayloadBuilder.Build(new
                {
                    @event = request.Suspend ? "membership_suspended" : "membership_reactivated",
                    tenantId = request.TenantId,
                    targetUserId = request.TargetUserId
                }),
                request.CallerUserId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
