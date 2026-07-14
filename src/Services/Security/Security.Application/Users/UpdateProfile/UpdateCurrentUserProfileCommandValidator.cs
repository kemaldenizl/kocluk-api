using FluentValidation;

namespace Security.Application.Users.UpdateProfile;

public sealed class UpdateCurrentUserProfileCommandValidator : AbstractValidator<UpdateCurrentUserProfileCommand>
{
    public UpdateCurrentUserProfileCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.FirstName is not null || x.LastName is not null || x.StudentType is not null)
            .WithMessage("At least one field must be provided.")
            .OverridePropertyName(nameof(UpdateCurrentUserProfileCommand.FirstName));

        RuleFor(x => x.FirstName!)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName!)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.LastName is not null);

        RuleFor(x => x.StudentType!.Value)
            .IsInEnum()
            .When(x => x.StudentType.HasValue);
    }
}
