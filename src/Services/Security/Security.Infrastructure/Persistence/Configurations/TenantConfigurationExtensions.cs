using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Abstractions;

namespace Security.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared configuration for tenant-scoped entities so every <see cref="ITenantScoped"/> mapping
/// declares its <c>TenantId</c> column consistently (required, non-nullable). Read isolation is
/// applied centrally through the global query filter in <c>SecurityDbContext</c>.
/// </summary>
internal static class TenantConfigurationExtensions
{
    public static EntityTypeBuilder<TEntity> HasTenantId<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITenantScoped
    {
        builder.Property(x => x.TenantId)
            .IsRequired();

        return builder;
    }
}
