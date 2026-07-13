using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;

namespace Security.Infrastructure.Messaging.Consumers;

/// <summary>
/// Retires the local tenant read model when the Tenant microservice deletes a tenant. Idempotent: a
/// tenant that is already gone is treated as success, so redelivery is safe. Once removed the tenant can
/// no longer be resolved on incoming requests.
/// </summary>
public sealed class TenantDeletedConsumer(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork,
    ILogger<TenantDeletedConsumer> logger)
    : IConsumer<TenantDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantDeletedIntegrationEvent> context)
    {
        var message = context.Message;

        var tenant = await tenantRepository.GetByIdAsync(message.TenantId, context.CancellationToken);
        if (tenant is null)
        {
            logger.LogInformation(
                "Tenant {TenantId} already absent; ignoring duplicate deleted event.",
                message.TenantId);
            return;
        }

        tenantRepository.Remove(tenant);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Retired tenant {TenantId} from the local read model.", tenant.Id);
    }
}
