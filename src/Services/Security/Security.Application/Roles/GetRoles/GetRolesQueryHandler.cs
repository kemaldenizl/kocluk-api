using MediatR;
using Security.Application.Abstractions.Memberships;
using Security.Application.Abstractions.Persistence;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;
using Security.Domain.Authorization;

namespace Security.Application.Roles.GetRoles;

public sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ITenantAccessAuthorizer tenantAccessAuthorizer)
    : IRequestHandler<GetRolesQuery, Result<IReadOnlyCollection<RoleDto>>>
{
    public async Task<Result<IReadOnlyCollection<RoleDto>>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var authorized = await tenantAccessAuthorizer.HasTenantPermissionAsync(
            request.CallerUserId, request.TenantId, PermissionCodes.RolesRead, cancellationToken);
        if (!authorized)
            return Result<IReadOnlyCollection<RoleDto>>.Failure(AccessErrors.Forbidden);

        var roles = await roleRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        var permissions = await permissionRepository.GetAllAsync(cancellationToken);

        var permissionCodesById = permissions.ToDictionary(x => x.Id, x => x.Code);

        var response = roles
            .OrderBy(x => x.Name)
            .Select(role => RoleMapper.ToDto(role, permissionCodesById))
            .ToArray();

        return Result<IReadOnlyCollection<RoleDto>>.Success(response);
    }
}
