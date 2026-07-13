using MediatR;
using Tenant.Application.Common.Results;
using Tenant.Application.Tenants.Dtos;

namespace Tenant.Application.Tenants.CreateTenant;

/// <summary>
/// Provisions a new tenant. <see cref="Slug"/> is optional: when omitted it is derived from
/// <see cref="Name"/>. The slug is the stable identifier other services resolve the tenant by.
/// </summary>
public sealed record CreateTenantCommand(string Name, string? Slug) : IRequest<Result<TenantDto>>;
