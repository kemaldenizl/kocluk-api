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

namespace Security.Application.Memberships.RemoveRoleFromMember;

public sealed class RemoveRoleFromMemberCommandHandler(
    ITenantAccessAuthorizer tenantAccessAuthorizer,
    IMembershipRepository membershipRepository,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<RemoveRoleFromMemberCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(RemoveRoleFromMemberCommand request, CancellationToken cancellationToken)
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
        membership.RemoveRole(request.RoleId, utcNow);

        await accessTokenRevocationStore.RevokeUserAsync(
            request.TargetUserId,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);

        await auditLogRepository.AddAsync(
            auditLogFactory.Create(
                AuditActionType.MembershipRoleRemoved,
                AuditPayloadBuilder.Build(new
                {
                    @event = "membership_role_removed",
                    tenantId = request.TenantId,
                    targetUserId = request.TargetUserId,
                    roleId = request.RoleId
                }),
                request.CallerUserId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
