using MediatR;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Auth.EmailChange.Dtos;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;

namespace Security.Application.Auth.EmailChange.ValidateEmailChange;

public sealed class ValidateEmailChangeQueryHandler(
    IUserRepository userRepository,
    IEmailChangeTokenRepository emailChangeTokenRepository,
    IEmailChangeTokenGenerator emailChangeTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ValidateEmailChangeQuery, Result<ValidateEmailChangeResponse>>
{
    public async Task<Result<ValidateEmailChangeResponse>> Handle(ValidateEmailChangeQuery request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var hashedToken = emailChangeTokenGenerator.Hash(request.Token);

        var changeToken = await emailChangeTokenRepository.GetByTokenHashAsync(
            hashedToken,
            cancellationToken);

        if (changeToken is null)
        {
            return Result<ValidateEmailChangeResponse>.Failure(AuthErrors.InvalidEmailChangeToken);
        }

        if (changeToken.Used)
        {
            return Result<ValidateEmailChangeResponse>.Failure(AuthErrors.UsedEmailChangeToken);
        }

        if (changeToken.IsExpired(utcNow))
        {
            return Result<ValidateEmailChangeResponse>.Failure(AuthErrors.ExpiredEmailChangeToken);
        }

        var user = await userRepository.GetByIdAsync(changeToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<ValidateEmailChangeResponse>.Failure(AuthErrors.InvalidEmailChangeToken);
        }

        return Result<ValidateEmailChangeResponse>.Success(new ValidateEmailChangeResponse(true, "Email change token is valid."));
    }
}
