namespace Tenant.API.Contracts.Tenants;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
