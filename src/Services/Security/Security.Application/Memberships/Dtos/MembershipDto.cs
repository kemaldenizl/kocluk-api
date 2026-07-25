namespace Security.Application.Memberships.Dtos;

/// <summary>A user's membership in a tenant, including status history and the roles held within it.</summary>
public sealed record MembershipDto(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime JoinedAtUtc,
    DateTime UpdatedAtUtc);
