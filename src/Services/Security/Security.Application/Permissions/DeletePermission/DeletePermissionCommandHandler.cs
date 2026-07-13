using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Permissions.DeletePermission;

public sealed class DeletePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePermissionCommand, Result>
{
    public async Task<Result> Handle(
        DeletePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
            return Result.Failure(PermissionErrors.NotFound);

        var isAssigned = await permissionRepository.IsAssignedToAnyRoleAsync(permission.Id, cancellationToken);
        if (isAssigned)
            return Result.Failure(PermissionErrors.InUse);

        permissionRepository.Remove(permission);

        var auditLog = auditLogFactory.Create(
            AuditActionType.PermissionDeleted,
            AuditPayloadBuilder.Build(new
            {
                @event = "permission_deleted",
                permissionId = permission.Id,
                permissionCode = permission.Code
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
