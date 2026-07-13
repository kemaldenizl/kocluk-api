using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Tokens;

namespace Security.Infrastructure.Persistence.Configurations.Tokens;

public sealed class PasswordChangeTokenConfiguration : IEntityTypeConfiguration<PasswordChangeToken>
{
    public void Configure(EntityTypeBuilder<PasswordChangeToken> builder)
    {
        builder.ToTable("password_change_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasTenantId();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.Used)
            .IsRequired();

        builder.Property(x => x.UsedAtUtc);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_password_change_tokens_token_hash");

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("ix_password_change_tokens_tenant_id_user_id");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_password_change_tokens_expires_at_utc");
    }
}
