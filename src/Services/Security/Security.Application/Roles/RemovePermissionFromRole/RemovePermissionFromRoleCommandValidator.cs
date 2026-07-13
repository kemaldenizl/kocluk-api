using FluentValidation;

namespace Security.Application.Roles.RemovePermissionFromRole;

public sealed class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
{
    public RemovePermissionFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}
