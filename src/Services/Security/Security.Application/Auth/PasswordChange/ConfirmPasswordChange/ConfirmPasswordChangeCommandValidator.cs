using FluentValidation;

namespace Security.Application.Auth.PasswordChange.ConfirmPasswordChange;

public sealed class ConfirmPasswordChangeCommandValidator : AbstractValidator<ConfirmPasswordChangeCommand>
{
    public ConfirmPasswordChangeCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(200)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");
    }
}
