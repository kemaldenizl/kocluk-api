using FluentValidation;

namespace Security.Application.Auth.PasswordChange.RequestPasswordChange;

public sealed class RequestPasswordChangeCommandValidator : AbstractValidator<RequestPasswordChangeCommand>
{
    public RequestPasswordChangeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .MaximumLength(200);
    }
}
