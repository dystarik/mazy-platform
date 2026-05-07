namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Configurations;

using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

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

        builder.Property(x => x.OwnerAccountId)
            .HasColumnName("owner_account_id")
            .IsRequired();
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.OwnerAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.PlatformType)
            .HasColumnName("platform_type")
            .IsRequired();

        builder.HasIndex(x => x.OwnerAccountId);
    }
}
