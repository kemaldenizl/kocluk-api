using MediatR;
using Tenant.Application.Common.Results;

namespace Tenant.Application.Tenants.RenameTenant;

public sealed record RenameTenantCommand(Guid TenantId, string Name) : IRequest<Result>;
