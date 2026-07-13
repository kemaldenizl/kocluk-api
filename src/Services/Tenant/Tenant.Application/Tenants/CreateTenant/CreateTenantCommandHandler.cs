using BuildingBlocks.Messaging.Abstractions;
using BuildingBlocks.Messaging.Contracts;
using MediatR;
using Tenant.Application.Abstractions.Persistence;
using Tenant.Application.Abstractions.Time;
using Tenant.Application.Abstractions.UnitOfWork;
using Tenant.Application.Common.Errors;
using Tenant.Application.Common.Results;
using Tenant.Application.Common.Slugs;
using Tenant.Application.Tenants.Dtos;
using DomainTenant = Tenant.Domain.Tenants.Tenant;

namespace Tenant.Application.Tenants.CreateTenant;

public sealed class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var source = string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug;
        var slug = SlugFactory.Create(source);
        if (string.IsNullOrEmpty(slug))
            return Result<TenantDto>.Failure(TenantErrors.SlugAlreadyExists);

        if (await tenantRepository.ExistsBySlugAsync(slug, cancellationToken))
            return Result<TenantDto>.Failure(TenantErrors.SlugAlreadyExists);

        var utcNow = dateTimeProvider.UtcNow;
        var tenant = new DomainTenant(Guid.NewGuid(), request.Name.Trim(), slug, utcNow);

        await tenantRepository.AddAsync(tenant, cancellationToken);

        // Published through the transactional outbox: the event is committed atomically with the tenant
        // row on SaveChanges and delivered to subscribers (Security, ...) reliably.
        await eventPublisher.PublishAsync(
            new TenantCreatedIntegrationEvent(tenant.Id, tenant.Name, tenant.Slug, utcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantDto>.Success(tenant.ToDto());
    }
}
