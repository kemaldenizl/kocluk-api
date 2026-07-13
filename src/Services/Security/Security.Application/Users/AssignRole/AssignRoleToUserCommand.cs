using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Users.AssignRole;

public sealed record AssignRoleToUserCommand(
    Guid UserId,
    Guid RoleId
) : IRequest<Result>;
