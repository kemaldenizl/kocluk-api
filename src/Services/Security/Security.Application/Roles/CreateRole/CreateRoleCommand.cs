using MediatR;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;

namespace Security.Application.Roles.CreateRole;

public sealed record CreateRoleCommand(
    Guid TenantId,
    Guid CallerUserId,
    string Name
) : IRequest<Result<RoleDto>>;
