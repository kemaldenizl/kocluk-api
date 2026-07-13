using MediatR;
using Tenant.Application.Common.Results;
using Tenant.Application.Tenants.Dtos;

namespace Tenant.Application.Tenants.GetTenantById;

public sealed record GetTenantByIdQuery(Guid TenantId) : IRequest<Result<TenantDto>>;
