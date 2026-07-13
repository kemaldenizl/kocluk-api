using FluentValidation;

namespace Security.Application.Permissions.DeletePermission;

public sealed class DeletePermissionCommandValidator : AbstractValidator<DeletePermissionCommand>
{
    public DeletePermissionCommandValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty();
    }
}
