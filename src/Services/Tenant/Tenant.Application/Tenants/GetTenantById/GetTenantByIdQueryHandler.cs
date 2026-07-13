using MediatR;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Common.Errors;
using Tenant.Application.Common.Results;
using Tenant.Application.Tenants.Dtos;

namespace Tenant.Application.Tenants.GetTenantById;

public sealed class GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);

        return tenant is null
            ? Result<TenantDto>.Failure(TenantErrors.NotFound)
            : Result<TenantDto>.Success(tenant.ToDto());
    }
}
