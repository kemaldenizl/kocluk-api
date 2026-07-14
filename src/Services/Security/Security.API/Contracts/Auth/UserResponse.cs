using Security.Domain.Users;

namespace Security.API.Contracts.Auth;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    StudentType StudentType,
    bool EmailVerified,
    bool IsActive
);
