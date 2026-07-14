using Security.Domain.Users;

namespace Security.API.Contracts.Users;

public sealed record UpdateCurrentUserRequest(
    string? FirstName,
    string? LastName,
    StudentType? StudentType
);
