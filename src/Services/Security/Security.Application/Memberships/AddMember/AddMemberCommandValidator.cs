using FluentValidation;

namespace Security.Application.Memberships.AddMember;

public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.UserId is not null || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Either a userId or an email must be supplied.");

        RuleForEach(x => x.RoleIds).NotEmpty();
    }
}
