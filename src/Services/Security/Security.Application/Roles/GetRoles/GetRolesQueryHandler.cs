using MediatR;
using Security.Application.Abstractions.Persistence;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;

namespace Security.Application.Roles.GetRoles;

public sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository)
    : IRequestHandler<GetRolesQuery, Result<IReadOnlyCollection<RoleDto>>>
{
    public async Task<Result<IReadOnlyCollection<RoleDto>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetAllAsync(cancellationToken);
        var permissions = await permissionRepository.GetAllAsync(cancellationToken);

        var permissionCodesById = permissions.ToDictionary(x => x.Id, x => x.Code);

        var response = roles
            .OrderBy(x => x.Name)
            .Select(role => RoleMapper.ToDto(role, permissionCodesById))
            .ToArray();

        return Result<IReadOnlyCollection<RoleDto>>.Success(response);
    }
}
