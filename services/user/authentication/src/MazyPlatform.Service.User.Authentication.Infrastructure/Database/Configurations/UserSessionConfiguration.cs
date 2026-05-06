namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

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

        builder.Property(x => x.RefreshTokenId)
            .HasColumnName("refresh_token_id")
            .IsRequired();
        builder.HasIndex(x => x.RefreshTokenId);

        builder.Property(x => x.RefreshTokenHash)
            .HasConversion(
                v => v.Value,
                v => RefreshTokenHash.FromTrusted(v))
            .HasColumnName("refresh_token_hash")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at")
            .IsRequired(false);

        builder.Property(x => x.RevokedReason)
            .HasConversion<int?>()
            .HasColumnName("revoked_reason")
            .IsRequired(false);
    }
}
