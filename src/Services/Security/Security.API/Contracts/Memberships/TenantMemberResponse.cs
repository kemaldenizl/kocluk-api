namespace Security.API.Contracts.Memberships;

public sealed record TenantMemberResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime JoinedAtUtc,
    DateTime UpdatedAtUtc
);
