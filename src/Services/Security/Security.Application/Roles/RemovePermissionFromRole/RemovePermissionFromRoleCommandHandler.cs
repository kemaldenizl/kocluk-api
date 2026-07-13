using MediatR;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Roles.RemovePermissionFromRole;

public sealed class RemovePermissionFromRoleCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<RemovePermissionFromRoleCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        RemovePermissionFromRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
            return Result.Failure(RoleErrors.PermissionNotFound);

        var utcNow = dateTimeProvider.UtcNow;

        role.RemovePermission(permission.Id);

        var affectedUserIds = await roleRepository.GetUserIdsByRoleIdAsync(role.Id, cancellationToken);

        foreach (var userId in affectedUserIds)
        {
            await accessTokenRevocationStore.RevokeUserAsync(
                userId,
                utcNow,
                utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
                cancellationToken);
        }

        var auditLog = auditLogFactory.Create(
            AuditActionType.PermissionRemovedFromRole,
            AuditPayloadBuilder.Build(new
            {
                @event = "permission_removed_from_role",
                roleId = role.Id,
                roleName = role.Name,
                permissionId = permission.Id,
                permissionCode = permission.Code,
                affectedUserCount = affectedUserIds.Count
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
