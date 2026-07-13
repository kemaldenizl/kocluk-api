using MediatR;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;

namespace Security.Application.Roles.GetRoles;

public sealed record GetRolesQuery : IRequest<Result<IReadOnlyCollection<RoleDto>>>;
