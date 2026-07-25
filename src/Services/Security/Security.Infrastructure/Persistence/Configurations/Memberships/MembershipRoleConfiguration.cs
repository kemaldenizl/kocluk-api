using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Memberships;

namespace Security.Infrastructure.Persistence.Configurations.Memberships;

public sealed class MembershipRoleConfiguration : IEntityTypeConfiguration<MembershipRole>
{
    public void Configure(EntityTypeBuilder<MembershipRole> builder)
    {
        builder.ToTable("membership_roles");

        builder.HasKey(x => new { x.MembershipId, x.RoleId });

        builder.Property(x => x.AssignedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("ix_membership_roles_role_id");
    }
}
