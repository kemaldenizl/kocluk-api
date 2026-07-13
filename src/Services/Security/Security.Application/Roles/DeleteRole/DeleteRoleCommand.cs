using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Roles.DeleteRole;

public sealed record DeleteRoleCommand(
    Guid RoleId
) : IRequest<Result>;
