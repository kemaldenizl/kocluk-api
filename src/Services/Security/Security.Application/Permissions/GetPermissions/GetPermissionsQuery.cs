using MediatR;
using Security.Application.Common.Results;
using Security.Application.Permissions.Dtos;

namespace Security.Application.Permissions.GetPermissions;

public sealed record GetPermissionsQuery : IRequest<Result<IReadOnlyCollection<PermissionDto>>>;
