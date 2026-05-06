namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Configurations;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EntitySchemaConfiguration : IEntityTypeConfiguration<EntitySchema>
{
    public void Configure(EntityTypeBuilder<EntitySchema> builder)
    {
        builder.ToTable("entity_schemas");

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

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => x.ProjectId);

        builder.HasMany(x => x.Fields)
            .WithOne()
            .HasForeignKey(x => x.SchemaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Fields).HasField("_fields").AutoInclude();
    }
}
