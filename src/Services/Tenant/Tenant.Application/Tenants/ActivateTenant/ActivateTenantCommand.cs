using MediatR;
using Tenant.Application.Common.Results;

namespace Tenant.Application.Tenants.ActivateTenant;

public sealed record ActivateTenantCommand(Guid TenantId) : IRequest<Result>;
