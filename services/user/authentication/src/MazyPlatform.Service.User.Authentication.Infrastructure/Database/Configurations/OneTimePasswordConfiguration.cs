namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.ValueObjects;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OneTimePasswordConfiguration : IEntityTypeConfiguration<OneTimePassword>
{
    public void Configure(EntityTypeBuilder<OneTimePassword> builder)
    {
        builder.ToTable("one_time_passwords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        builder.Property(x => x.UserAccountId)
            .HasColumnName("user_account_id")
            .IsRequired();

        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Code)
            .HasConversion(
                v => v.Value,
                v => OtpCodeHash.FromTrusted(v))
            .HasColumnName("code_hash")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.FailedAttempts)
            .HasColumnName("failed_attempts")
            .IsRequired();

        builder.Property(x => x.VerifiedAt)
            .HasColumnName("verified_at")
            .IsRequired(false);

        builder.Property(x => x.InvalidatedAt)
            .HasColumnName("invalidated_at")
            .IsRequired(false);
    }
}
