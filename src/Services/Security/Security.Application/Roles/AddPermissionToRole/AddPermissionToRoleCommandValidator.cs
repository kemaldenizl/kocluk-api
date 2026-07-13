using FluentValidation;

namespace Security.Application.Roles.AddPermissionToRole;

public sealed class AddPermissionToRoleCommandValidator : AbstractValidator<AddPermissionToRoleCommand>
{
    public AddPermissionToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}
