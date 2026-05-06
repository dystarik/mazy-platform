namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Configurations;

using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EntityFieldConfiguration : IEntityTypeConfiguration<EntityField>
{
    public void Configure(EntityTypeBuilder<EntityField> builder)
    {
        builder.ToTable("entity_fields");

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

        builder.Property(x => x.SchemaId)
            .HasColumnName("schema_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.FieldType)
            .HasColumnName("field_type")
            .IsRequired();

        builder.Property(x => x.IsRequired)
            .HasColumnName("is_required")
            .IsRequired();

        builder.Property(x => x.DefaultValue)
            .HasColumnName("default_value")
            .IsRequired(false);

        builder.HasIndex(x => x.SchemaId);
    }
}
