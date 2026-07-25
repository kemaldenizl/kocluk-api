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

namespace Security.Application.Memberships.AssignRoleToMember;

public sealed class AssignRoleToMemberCommandHandler(
    ITenantAccessAuthorizer tenantAccessAuthorizer,
    IMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<AssignRoleToMemberCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(AssignRoleToMemberCommand request, CancellationToken cancellationToken)
    {
        var authorized = await tenantAccessAuthorizer.HasTenantPermissionAsync(
            request.CallerUserId, request.TenantId, PermissionCodes.MembersManage, cancellationToken);
        if (!authorized)
            return Result.Failure(AccessErrors.Forbidden);

        var membership = await membershipRepository.GetByUserAndTenantAsync(
            request.TargetUserId, request.TenantId, cancellationToken);
        if (membership is null)
            return Result.Failure(MembershipErrors.NotFound);

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null || role.TenantId != request.TenantId)
            return Result.Failure(MembershipErrors.RoleNotInTenant);

        var utcNow = dateTimeProvider.UtcNow;
        membership.AddRole(role.Id, utcNow);

        await accessTokenRevocationStore.RevokeUserAsync(
            request.TargetUserId,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);

        await auditLogRepository.AddAsync(
            auditLogFactory.Create(
                AuditActionType.MembershipRoleAssigned,
                AuditPayloadBuilder.Build(new
                {
                    @event = "membership_role_assigned",
                    tenantId = request.TenantId,
                    targetUserId = request.TargetUserId,
                    roleId = role.Id,
                    roleName = role.Name
                }),
                request.CallerUserId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
