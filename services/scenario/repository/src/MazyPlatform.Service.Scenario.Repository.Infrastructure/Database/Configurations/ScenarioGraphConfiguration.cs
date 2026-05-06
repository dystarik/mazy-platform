namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Configurations;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ScenarioGraphConfiguration : IEntityTypeConfiguration<ScenarioGraph>
{
    public void Configure(EntityTypeBuilder<ScenarioGraph> builder)
    {
        builder.ToTable("scenario_graphs");

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

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.HasIndex(x => x.ProjectId).IsUnique();

        builder.Property(x => x.DraftJson)
            .HasColumnName("draft_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CurrentReleaseVersion)
            .HasColumnName("current_release_version")
            .IsRequired(false);

        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey(x => x.ScenarioGraphId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Versions).HasField("_versions").AutoInclude();
    }
}
