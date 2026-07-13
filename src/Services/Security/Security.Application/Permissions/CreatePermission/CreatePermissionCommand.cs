using MediatR;
using Security.Application.Common.Results;
using Security.Application.Permissions.Dtos;

namespace Security.Application.Permissions.CreatePermission;

public sealed record CreatePermissionCommand(
    string Code
) : IRequest<Result<PermissionDto>>;
