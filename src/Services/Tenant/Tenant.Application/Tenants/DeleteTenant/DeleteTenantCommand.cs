using MediatR;
using Tenant.Application.Common.Results;

namespace Tenant.Application.Tenants.DeleteTenant;

public sealed record DeleteTenantCommand(Guid TenantId) : IRequest<Result>;
