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

namespace Security.Application.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;

        var affectedUserIds = await roleRepository.GetUserIdsByRoleIdAsync(role.Id, cancellationToken);

        roleRepository.Remove(role);

        foreach (var userId in affectedUserIds)
        {
            await accessTokenRevocationStore.RevokeUserAsync(
                userId,
                utcNow,
                utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
                cancellationToken);
        }

        var auditLog = auditLogFactory.Create(
            AuditActionType.RoleDeleted,
            AuditPayloadBuilder.Build(new
            {
                @event = "role_deleted",
                roleId = role.Id,
                roleName = role.Name,
                affectedUserCount = affectedUserIds.Count
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
