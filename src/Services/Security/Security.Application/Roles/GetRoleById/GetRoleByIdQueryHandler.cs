using MediatR;
using Security.Application.Abstractions.Memberships;
using Security.Application.Abstractions.Persistence;
using Security.Application.Common.Authorization;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;
using Security.Domain.Authorization;

namespace Security.Application.Roles.GetRoleById;

public sealed class GetRoleByIdQueryHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ITenantAccessAuthorizer tenantAccessAuthorizer)
    : IRequestHandler<GetRoleByIdQuery, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result<RoleDto>.Failure(RoleErrors.NotFound);

        var authorized = await RoleAccessGuard.CanActAsync(
            tenantAccessAuthorizer, role, request.CallerUserId, PermissionCodes.RolesRead, cancellationToken);
        if (!authorized)
            return Result<RoleDto>.Failure(AccessErrors.Forbidden);

        var permissions = await permissionRepository.GetAllAsync(cancellationToken);
        var permissionCodesById = permissions.ToDictionary(x => x.Id, x => x.Code);

        var response = RoleMapper.ToDto(role, permissionCodesById);

        return Result<RoleDto>.Success(response);
    }
}
