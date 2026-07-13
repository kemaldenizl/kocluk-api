using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Abstractions.Time;
using Tenant.Application.Abstractions.UnitOfWork;
using Tenant.Application.Common.Errors;
using Tenant.Application.Common.Results;

namespace Tenant.Application.Tenants.RenameTenant;

public sealed class RenameTenantCommandHandler(
    ITenantRepository tenantRepository,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RenameTenantCommand, Result>
{
    public async Task<Result> Handle(RenameTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound);

        var utcNow = dateTimeProvider.UtcNow;
        if (!tenant.Rename(request.Name, utcNow))
            return Result.Success();

        await eventPublisher.PublishAsync(
            new TenantUpdatedIntegrationEvent(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, utcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
