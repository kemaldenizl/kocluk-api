using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Tokens;

namespace Security.Infrastructure.Persistence.Configurations.Tokens;

public sealed class EmailChangeTokenConfiguration : IEntityTypeConfiguration<EmailChangeToken>
{
    public void Configure(EntityTypeBuilder<EmailChangeToken> builder)
    {
        builder.ToTable("email_change_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

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
            .HasDatabaseName("ix_email_change_tokens_token_hash");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_email_change_tokens_user_id");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_email_change_tokens_expires_at_utc");
    }
}
