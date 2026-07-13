namespace Tenant.Application.Tenants.Dtos;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
