using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Authorization;

namespace Security.Infrastructure.Persistence.Configurations.Authorization;

public sealed class UserPlatformRoleConfiguration : IEntityTypeConfiguration<UserPlatformRole>
{
    public void Configure(EntityTypeBuilder<UserPlatformRole> builder)
    {
        builder.ToTable("user_platform_roles");

        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.Property(x => x.AssignedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("ix_user_platform_roles_role_id");
    }
}
