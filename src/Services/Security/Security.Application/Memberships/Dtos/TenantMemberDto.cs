namespace Security.Application.Memberships.Dtos;

/// <summary>A member of a tenant, projected together with the user's identity for member listings.</summary>
public sealed record TenantMemberDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime JoinedAtUtc,
    DateTime UpdatedAtUtc);
