using MediatR;
using Security.Application.Common.Results;
using Security.Application.Users.GetCurrentUser;
using Security.Domain.Users;

namespace Security.Application.Users.UpdateProfile;

public sealed record UpdateCurrentUserProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    StudentType? StudentType
) : IRequest<Result<CurrentUserProfileDto>>;
