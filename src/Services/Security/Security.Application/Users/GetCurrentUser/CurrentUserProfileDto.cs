using Security.Domain.Users;

namespace Security.Application.Users.GetCurrentUser;

public sealed record CurrentUserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    StudentType StudentType,
    bool EmailVerified,
    bool IsActive
);
