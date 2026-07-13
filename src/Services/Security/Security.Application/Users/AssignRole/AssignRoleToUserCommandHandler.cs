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

namespace Security.Application.Users.AssignRole;

public sealed class AssignRoleToUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<AssignRoleToUserCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        AssignRoleToUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;

        user.AddRole(role.Id, utcNow);

        await accessTokenRevocationStore.RevokeUserAsync(
            user.Id,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.RoleAssigned,
            AuditPayloadBuilder.Build(new
            {
                @event = "role_assigned",
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
