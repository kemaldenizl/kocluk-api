using FluentValidation;

namespace Security.Application.Auth.EmailChange.ValidateEmailChange;

public sealed class ValidateEmailChangeQueryValidator : AbstractValidator<ValidateEmailChangeQuery>
{
    public ValidateEmailChangeQueryValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
