using MediatR;
using Security.Application.Abstractions.Persistence;
using Security.Application.Common.Results;
using Security.Application.Permissions.Dtos;

namespace Security.Application.Permissions.GetPermissions;

public sealed class GetPermissionsQueryHandler(
    IPermissionRepository permissionRepository)
    : IRequestHandler<GetPermissionsQuery, Result<IReadOnlyCollection<PermissionDto>>>
{
    public async Task<Result<IReadOnlyCollection<PermissionDto>>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.GetAllAsync(cancellationToken);

        var response = permissions
            .OrderBy(x => x.Code)
            .Select(x => new PermissionDto(x.Id, x.Code))
            .ToArray();

        return Result<IReadOnlyCollection<PermissionDto>>.Success(response);
    }
}
