namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations.UserAccounts;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ExternalProviderConfiguration : IEntityTypeConfiguration<UserLinkedProviders>
{
    public void Configure(EntityTypeBuilder<UserLinkedProviders> builder)
    {
        builder.ToTable("user_linked_providers");

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

        builder.HasOne<UserAccount>()
            .WithOne(x => x.UserLinkedProviders)
            .HasForeignKey<UserLinkedProviders>(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserAccountId)
            .IsUnique();

        builder.OwnsMany(x => x.LinkedProviders, ownedBuilder =>
        {
            ownedBuilder.ToTable("user_external_providers");

            ownedBuilder.WithOwner()
                .HasForeignKey("user_linked_providers_id");

            ownedBuilder.HasKey("user_linked_providers_id", nameof(ExternalProvider.Type));

            ownedBuilder.Property("user_linked_providers_id")
                .HasColumnName("user_linked_providers_id");

            ownedBuilder.Property(x => x.Type)
                .HasColumnName("type")
                .HasConversion<int>()
                .IsRequired();

            ownedBuilder.Property(x => x.Email)
                .HasConversion(
                    v => v.Value,
                    v => Email.FromTrusted(v))
                .HasColumnName("email")
                .IsRequired();
        });

        builder.Navigation(x => x.LinkedProviders).AutoInclude();
    }
}
