using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Roles.RemovePermissionFromRole;

public sealed record RemovePermissionFromRoleCommand(
    Guid RoleId,
    Guid CallerUserId,
    Guid PermissionId
) : IRequest<Result>;
