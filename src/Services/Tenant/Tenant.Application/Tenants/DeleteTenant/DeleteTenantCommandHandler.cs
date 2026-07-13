using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Abstractions.Time;
using Tenant.Application.Abstractions.UnitOfWork;
using Tenant.Application.Common.Errors;
using Tenant.Application.Common.Results;

namespace Tenant.Application.Tenants.DeleteTenant;

public sealed class DeleteTenantCommandHandler(
    ITenantRepository tenantRepository,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
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
