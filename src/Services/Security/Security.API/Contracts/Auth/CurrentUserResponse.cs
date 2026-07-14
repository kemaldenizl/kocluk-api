using Security.Domain.Users;

namespace Security.API.Contracts.Auth;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    StudentType StudentType,
    bool EmailVerified,
    Guid? SessionId,
    string? AccessTokenJti,
    IReadOnlyCollection<string> Permissions
);
