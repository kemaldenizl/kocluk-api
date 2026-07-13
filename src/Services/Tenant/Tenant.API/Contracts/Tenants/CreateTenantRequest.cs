namespace Tenant.API.Contracts.Tenants;

/// <summary>Payload to provision a tenant. <c>Slug</c> is optional and derived from <c>Name</c> when omitted.</summary>
public sealed record CreateTenantRequest(string Name, string? Slug);
