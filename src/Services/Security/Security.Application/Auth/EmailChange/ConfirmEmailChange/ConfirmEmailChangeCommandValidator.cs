using FluentValidation;

namespace Security.Application.Auth.EmailChange.ConfirmEmailChange;

public sealed class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();
    }
}
