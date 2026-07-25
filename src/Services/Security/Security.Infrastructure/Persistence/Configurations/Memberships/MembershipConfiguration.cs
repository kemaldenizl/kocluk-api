using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Memberships;

namespace Security.Infrastructure.Persistence.Configurations.Memberships;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.JoinedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        // A user has at most one membership row per tenant (reactivated on re-join, preserving history).
        builder.HasIndex(x => new { x.UserId, x.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_memberships_user_id_tenant_id");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_memberships_tenant_id");

        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.Roles)
            .WithOne()
            .HasForeignKey(x => x.MembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
