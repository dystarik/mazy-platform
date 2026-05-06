namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations.UserAccounts;

using System.Text.Json;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MfaSettingsConfiguration : IEntityTypeConfiguration<MfaSettings>
{
    public void Configure(EntityTypeBuilder<MfaSettings> builder)
    {
        builder.ToTable("mfa_settings");

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
            .WithOne(x => x.MfaSettings)
            .HasForeignKey<MfaSettings>(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        var backupCodesProperty = builder.Property(x => x.BackupCodes)
            .HasColumnName("backup_codes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(c => c.Value).ToList()),
                v => JsonSerializer.Deserialize<List<string>>(v)!.ConvertAll(BackupCodeHash.FromTrusted))
            .IsRequired();

        backupCodesProperty.Metadata.SetValueComparer(new ValueComparer<IReadOnlyCollection<BackupCodeHash>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()));

        builder.Navigation(x => x.MfaMethods).AutoInclude();

        builder.Ignore(x => x.TotpMethod);
        builder.Ignore(x => x.EmailMethod);
    }
}
