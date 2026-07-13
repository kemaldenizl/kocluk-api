using MassTransit;
using Microsoft.Extensions.Logging;
using Security.Application.Abstractions.Messaging.IntegrationEvents;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Domain.Tenancy;

namespace Security.Infrastructure.Messaging.Consumers;

/// <summary>
/// Projects <see cref="TenantCreatedIntegrationEvent"/> from the Tenant microservice into the local
/// tenant read model. Idempotent: a tenant that already exists is left untouched, so redelivery is safe.
/// </summary>
public sealed class TenantCreatedConsumer(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<TenantCreatedConsumer> logger)
    : IConsumer<TenantCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        var existing = await tenantRepository.GetByIdAsync(message.TenantId, context.CancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "Tenant {TenantId} already provisioned; ignoring duplicate created event.",
                message.TenantId);
            return;
        }

        var tenant = new Tenant(
            message.TenantId,
            message.Name,
            message.Slug.Trim().ToLowerInvariant(),
            dateTimeProvider.UtcNow);

        await tenantRepository.AddAsync(tenant, context.CancellationToken);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Provisioned tenant {TenantId} ({Slug}).", tenant.Id, tenant.Slug);
    }
}
