using Security.Domain.Users;

namespace Security.Application.Auth.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    StudentType StudentType,
    bool EmailVerified,
    bool IsActive
);
