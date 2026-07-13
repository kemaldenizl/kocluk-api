using MediatR;
using Tenant.Application.Common.Results;
using Tenant.Application.Tenants.Dtos;

namespace Tenant.Application.Tenants.GetTenants;

public sealed record GetTenantsQuery : IRequest<Result<IReadOnlyCollection<TenantDto>>>;
