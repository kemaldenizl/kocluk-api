using MediatR;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Common.Results;
using Tenant.Application.Tenants.Dtos;

namespace Tenant.Application.Tenants.GetTenants;

public sealed class GetTenantsQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantsQuery, Result<IReadOnlyCollection<TenantDto>>>
{
    public async Task<Result<IReadOnlyCollection<TenantDto>>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllAsync(cancellationToken);

        var response = tenants
            .OrderBy(x => x.Name)
            .Select(TenantMapper.ToDto)
            .ToArray();

        return Result<IReadOnlyCollection<TenantDto>>.Success(response);
    }
}
