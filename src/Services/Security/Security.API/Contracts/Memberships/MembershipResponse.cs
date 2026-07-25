namespace Security.API.Contracts.Memberships;

public sealed record MembershipResponse(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime JoinedAtUtc,
    DateTime UpdatedAtUtc
);
