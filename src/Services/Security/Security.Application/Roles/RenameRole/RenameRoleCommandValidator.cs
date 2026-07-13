using FluentValidation;

namespace Security.Application.Roles.RenameRole;

public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand>
{
    public RenameRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
