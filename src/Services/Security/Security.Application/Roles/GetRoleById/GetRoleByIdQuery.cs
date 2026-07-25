using MediatR;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;

namespace Security.Application.Roles.GetRoleById;

public sealed record GetRoleByIdQuery(
    Guid RoleId,
    Guid CallerUserId
) : IRequest<Result<RoleDto>>;
