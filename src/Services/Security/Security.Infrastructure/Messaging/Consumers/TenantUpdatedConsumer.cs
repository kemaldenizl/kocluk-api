using MassTransit;
using Microsoft.Extensions.Logging;
using Security.Application.Abstractions.Messaging.IntegrationEvents;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Domain.Tenancy;

namespace Security.Infrastructure.Messaging.Consumers;

/// <summary>
/// Keeps the local tenant read model in sync with <see cref="TenantUpdatedIntegrationEvent"/> from the
/// Tenant microservice. Idempotent and self-healing: if the tenant is not yet known locally (a missed
/// created event) it is provisioned from the event's full state.
/// </summary>
public sealed class TenantUpdatedConsumer(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<TenantUpdatedConsumer> logger)
    : IConsumer<TenantUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantUpdatedIntegrationEvent> context)
    {
        var message = context.Message;

        var tenant = await tenantRepository.GetByIdAsync(message.TenantId, context.CancellationToken);

        if (tenant is null)
        {
            tenant = new Tenant(
                message.TenantId,
                message.Name,
                message.Slug.Trim().ToLowerInvariant(),
                dateTimeProvider.UtcNow);

            ApplyActiveState(tenant, message.IsActive);

            await tenantRepository.AddAsync(tenant, context.CancellationToken);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogWarning(
                "Received update for unknown tenant {TenantId}; provisioned it from the event state.",
                message.TenantId);
            return;
        }

        tenant.Rename(message.Name);
        ApplyActiveState(tenant, message.IsActive);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Synced tenant {TenantId} (active: {IsActive}).", tenant.Id, message.IsActive);
    }

    private static void ApplyActiveState(Tenant tenant, bool isActive)
    {
        if (isActive)
            tenant.Activate();
        else
            tenant.Deactivate();
    }
}
