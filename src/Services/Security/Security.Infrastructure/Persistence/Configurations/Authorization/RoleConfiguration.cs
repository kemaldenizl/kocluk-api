using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Authorization;

namespace Security.Infrastructure.Persistence.Configurations.Authorization;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Null for platform-scoped roles; set for tenant-scoped roles.
        builder.Property(x => x.TenantId);

        builder.Property(x => x.Scope)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(200)
            .IsRequired();

        // Tenant roles: unique per (tenant, name). Only applies to rows that have a tenant.
        builder.HasIndex(x => new { x.TenantId, x.NormalizedName })
            .IsUnique()
            .HasFilter("\"TenantId\" IS NOT NULL")
            .HasDatabaseName("ix_roles_tenant_id_normalized_name");

        // Platform roles: unique per name (tenant-independent).
        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasFilter("\"TenantId\" IS NULL")
            .HasDatabaseName("ix_roles_platform_normalized_name");

        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.Permissions)
            .WithOne()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Permissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
