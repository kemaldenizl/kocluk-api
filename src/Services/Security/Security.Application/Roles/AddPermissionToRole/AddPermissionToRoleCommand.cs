using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Roles.AddPermissionToRole;

public sealed record AddPermissionToRoleCommand(
    Guid RoleId,
    Guid CallerUserId,
    Guid PermissionId
) : IRequest<Result>;
