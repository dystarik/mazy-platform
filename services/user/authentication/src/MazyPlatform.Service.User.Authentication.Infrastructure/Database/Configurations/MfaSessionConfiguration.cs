namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MfaSessionConfiguration : IEntityTypeConfiguration<MfaSession>
{
    public void Configure(EntityTypeBuilder<MfaSession> builder)
    {
        builder.ToTable("mfa_sessions");

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

        builder.Property(x => x.Action)
            .HasConversion<int>()
            .HasColumnName("action")
            .IsRequired();

        builder.Property(x => x.RequiredFactorCount)
            .HasColumnName("required_factor_count")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.ValidUntil)
            .HasColumnName("valid_until")
            .IsRequired(false);

        builder.Property(x => x.IsCompletedByBackupCode)
            .HasColumnName("is_completed_by_backup_code")
            .IsRequired();

        builder.PrimitiveCollection(x => x.CompletedFactors)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("completed_factors")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Ignore(x => x.IsCompleted);
    }
}
