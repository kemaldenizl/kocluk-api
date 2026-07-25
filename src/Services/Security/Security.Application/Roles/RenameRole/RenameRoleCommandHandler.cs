using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Memberships;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Authorization;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Authorization;

namespace Security.Application.Roles.RenameRole;

public sealed class RenameRoleCommandHandler(
    IRoleRepository roleRepository,
    ITenantAccessAuthorizer tenantAccessAuthorizer,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RenameRoleCommand, Result>
{
    public async Task<Result> Handle(
        RenameRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var authorized = await RoleAccessGuard.CanActAsync(
            tenantAccessAuthorizer, role, request.CallerUserId, PermissionCodes.RolesManage, cancellationToken);
        if (!authorized)
            return Result.Failure(AccessErrors.Forbidden);

        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();

        if (!string.Equals(role.NormalizedName, normalizedName, StringComparison.Ordinal) && role.TenantId is not null)
        {
            var alreadyExists = await roleRepository.ExistsByTenantAndNormalizedNameAsync(
                role.TenantId.Value, normalizedName, cancellationToken);
            if (alreadyExists)
                return Result.Failure(RoleErrors.AlreadyExists);
        }

        role.Rename(name, normalizedName);

        var auditLog = auditLogFactory.Create(
            AuditActionType.RoleRenamed,
            AuditPayloadBuilder.Build(new
            {
                @event = "role_renamed",
                roleId = role.Id,
                roleName = role.Name
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
