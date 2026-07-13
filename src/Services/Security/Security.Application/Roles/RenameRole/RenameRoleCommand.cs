using MediatR;
using Security.Application.Common.Results;

namespace Security.Application.Roles.RenameRole;

public sealed record RenameRoleCommand(
    Guid RoleId,
    string Name
) : IRequest<Result>;
