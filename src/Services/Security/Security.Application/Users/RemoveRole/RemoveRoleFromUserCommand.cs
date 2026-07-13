using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Users.RemoveRole;

public sealed record RemoveRoleFromUserCommand(
    Guid UserId,
    Guid RoleId
) : IRequest<Result>;
