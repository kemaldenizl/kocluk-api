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

namespace Security.Application.Users.RemoveRole;

public sealed class RemoveRoleFromUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<RemoveRoleFromUserCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        RemoveRoleFromUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;

        user.RemoveRole(role.Id);

        await accessTokenRevocationStore.RevokeUserAsync(
            user.Id,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.RoleRemoved,
            AuditPayloadBuilder.Build(new
            {
                @event = "role_removed",
                targetUserId = user.Id,
                roleId = role.Id,
                roleName = role.Name
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
