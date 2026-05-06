namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations.UserAccounts;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

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

        builder.Property(x => x.Email)
            .HasConversion(
                v => v.Value,
                v => Email.FromTrusted(v))
            .HasColumnName("email")
            .IsRequired();
        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.EmailVerifiedAt)
            .HasColumnName("email_verified_at")
            .IsRequired(false);

        builder.Property(x => x.PasswordHash)
            .HasConversion(
                v => v == null ? null : v.Value,
                v => v == null ? null : PasswordHash.FromTrusted(v))
            .HasColumnName("password_hash")
            .IsRequired(false);

        builder.Property(x => x.PasswordSetAt)
            .HasColumnName("password_set_at")
            .IsRequired(false);

        builder.Navigation(x => x.MfaSettings).AutoInclude();
        builder.Navigation(x => x.UserLinkedProviders).AutoInclude();
    }
}
