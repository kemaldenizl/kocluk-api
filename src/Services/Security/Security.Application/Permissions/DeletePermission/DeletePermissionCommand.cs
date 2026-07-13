using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Permissions.DeletePermission;

public sealed record DeletePermissionCommand(
    Guid PermissionId
) : IRequest<Result>;
