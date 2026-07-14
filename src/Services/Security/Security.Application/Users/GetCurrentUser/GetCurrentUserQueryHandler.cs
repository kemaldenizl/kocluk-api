using MediatR;
using Security.Application.Abstractions.Persistence;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;

namespace Security.Application.Users.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserProfileDto>>
{
    public async Task<Result<CurrentUserProfileDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<CurrentUserProfileDto>.Failure(UserErrors.NotFound);
        }

        var profile = new CurrentUserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentType,
            user.EmailVerified,
            user.IsActive);

        return Result<CurrentUserProfileDto>.Success(profile);
    }
}
