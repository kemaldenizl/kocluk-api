using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Permissions.Dtos;
using Security.Domain.Auditing;
using Security.Domain.Authorization;

namespace Security.Application.Permissions.CreatePermission;

public sealed class CreatePermissionCommandHandler(
    IPermissionRepository permissionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePermissionCommand, Result<PermissionDto>>
{
    public async Task<Result<PermissionDto>> Handle(
        CreatePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToLowerInvariant();

        var alreadyExists = await permissionRepository.ExistsByCodeAsync(code, cancellationToken);
        if (alreadyExists)
            return Result<PermissionDto>.Failure(PermissionErrors.AlreadyExists);

        var permission = new Permission(Guid.NewGuid(), code);

        await permissionRepository.AddAsync(permission, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.PermissionCreated,
            AuditPayloadBuilder.Build(new
            {
                @event = "permission_created",
                permissionId = permission.Id,
                permissionCode = permission.Code
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new PermissionDto(permission.Id, permission.Code);

        return Result<PermissionDto>.Success(response);
    }
}
