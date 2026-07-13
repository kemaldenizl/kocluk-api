using FluentValidation;

namespace Security.Application.Auth.EmailChange.RequestEmailChange;

public sealed class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .MaximumLength(200);
    }
}
