namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Configurations;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ScenarioVersionConfiguration : IEntityTypeConfiguration<ScenarioVersion>
{
    public void Configure(EntityTypeBuilder<ScenarioVersion> builder)
    {
        builder.ToTable("scenario_versions");

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

        builder.Property(x => x.ScenarioGraphId)
            .HasColumnName("scenario_graph_id")
            .IsRequired();

        builder.Property(x => x.GraphJson)
            .HasColumnName("graph_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.HasIndex(x => new { x.ScenarioGraphId, x.Version }).IsUnique();
    }
}
