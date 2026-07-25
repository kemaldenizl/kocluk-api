namespace Security.API.Contracts.Memberships;

/// <summary>Adds an existing user to a tenant. Provide either <see cref="UserId"/> or <see cref="Email"/>.</summary>
public sealed record AddMemberRequest(
    Guid TenantId,
    Guid? UserId,
    string? Email,
    IReadOnlyList<Guid>? RoleIds
);
