using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Microsoft.Extensions.Options;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Abstractions.Time;
using Tenant.Application.Abstractions.UnitOfWork;
using Tenant.Application.Common.Errors;
using Tenant.Application.Common.Results;
using Tenant.Application.Common.Tenancy;

namespace Tenant.Application.Tenants.DeleteTenant;

public sealed class DeleteTenantCommandHandler(
    ITenantRepository tenantRepository,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IOptions<TenantDefaultsOptions> tenantDefaults,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    private readonly TenantDefaultsOptions _tenantDefaults = tenantDefaults.Value;

    public async Task<Result> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var publicTenantId = _tenantDefaults.PublicTenantId == Guid.Empty
            ? TenantDefaultsOptions.PublicTenantIdValue
            : _tenantDefaults.PublicTenantId;

        if (request.TenantId == publicTenantId)
            return Result.Failure(TenantErrors.CannotDeletePublicTenant);

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound);

        tenantRepository.Remove(tenant);

        await eventPublisher.PublishAsync(
            new TenantDeletedIntegrationEvent(tenant.Id, dateTimeProvider.UtcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
