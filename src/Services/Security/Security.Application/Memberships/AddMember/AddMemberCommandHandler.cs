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
using Security.Application.Memberships.Dtos;
using Security.Domain.Auditing;
using Security.Domain.Authorization;
using Security.Domain.Memberships;

namespace Security.Application.Memberships.AddMember;

public sealed class AddMemberCommandHandler(
    ITenantAccessAuthorizer tenantAccessAuthorizer,
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IMembershipRepository membershipRepository,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<AddMemberCommand, Result<MembershipDto>>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result<MembershipDto>> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var authorized = await tenantAccessAuthorizer.HasTenantPermissionAsync(
            request.CallerUserId, request.TenantId, PermissionCodes.MembersManage, cancellationToken);
        if (!authorized)
            return Result<MembershipDto>.Failure(AccessErrors.Forbidden);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result<MembershipDto>.Failure(TenantErrors.NotFound);
        if (!tenant.IsActive)
            return Result<MembershipDto>.Failure(TenantErrors.Inactive);

        var targetUser = request.UserId is Guid userId
            ? await userRepository.GetByIdAsync(userId, cancellationToken)
            : await userRepository.GetByNormalizedEmailAsync(request.Email!.Trim().ToUpperInvariant(), cancellationToken);

        if (targetUser is null)
            return Result<MembershipDto>.Failure(UserErrors.NotFound);

        var tenantRoles = await roleRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        var roleById = tenantRoles.ToDictionary(r => r.Id);

        var requestedRoleIds = request.RoleIds.Distinct().ToList();
        if (requestedRoleIds.Any(id => !roleById.ContainsKey(id)))
            return Result<MembershipDto>.Failure(MembershipErrors.RoleNotInTenant);

        var utcNow = dateTimeProvider.UtcNow;

        var membership = await membershipRepository.GetByUserAndTenantAsync(
            targetUser.Id, request.TenantId, cancellationToken);

        if (membership is null)
        {
            membership = new Membership(Guid.NewGuid(), targetUser.Id, request.TenantId, utcNow);
            foreach (var roleId in requestedRoleIds)
                membership.AddRole(roleId, utcNow);

            await membershipRepository.AddAsync(membership, cancellationToken);
        }
        else
        {
            if (membership.Status == MembershipStatus.Active && requestedRoleIds.Count == 0)
                return Result<MembershipDto>.Failure(MembershipErrors.AlreadyActive);

            membership.Reactivate(utcNow);
            foreach (var roleId in requestedRoleIds)
                membership.AddRole(roleId, utcNow);
        }

        await RevokeTokensAsync(targetUser.Id, utcNow, cancellationToken);

        await auditLogRepository.AddAsync(
            auditLogFactory.Create(
                AuditActionType.MembershipAdded,
                AuditPayloadBuilder.Build(new
                {
                    @event = "membership_added",
                    tenantId = request.TenantId,
                    targetUserId = targetUser.Id,
                    roleIds = requestedRoleIds
                }),
                request.CallerUserId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var roleNames = membership.Roles
            .Where(r => roleById.ContainsKey(r.RoleId))
            .Select(r => roleById[r.RoleId].Name)
            .ToList();

        var dto = new MembershipDto(
            membership.Id,
            membership.UserId,
            membership.TenantId,
            membership.Status.ToString(),
            roleNames,
            membership.JoinedAtUtc,
            membership.UpdatedAtUtc);

        return Result<MembershipDto>.Success(dto);
    }

    private async Task RevokeTokensAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await accessTokenRevocationStore.RevokeUserAsync(
            userId,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);
    }
}
