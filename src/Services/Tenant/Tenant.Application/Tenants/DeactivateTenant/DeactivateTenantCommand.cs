using MediatR;
using Tenant.Application.Common.Results;

namespace Tenant.Application.Tenants.DeactivateTenant;

public sealed record DeactivateTenantCommand(Guid TenantId) : IRequest<Result>;
