namespace MazyPlatform.Scenario.Storage.Mongo.Stores;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Storage.Mongo.Documents;

using MongoDB.Driver;

internal sealed class MongoSchemaStore(IMongoDatabase database) : ISchemaStore
{
    private readonly IMongoCollection<EntitySchemaDocument> _schemas = database.GetCollection<EntitySchemaDocument>("entity_schemas");

    public async Task<EntitySchema?> GetAsync(
        Guid projectId,
        int scenarioVersion,
        string entityName,
        CancellationToken cancellationToken = default)
    {
        var document = await _schemas
            .Find(x => x.ProjectId == projectId
                && x.ScenarioVersion == scenarioVersion
                && x.Name == entityName)
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : ToEntitySchema(document);
    }

    public async Task<EntitySchema?> GetAsync(
        Guid projectId,
        string entityName,
        CancellationToken cancellationToken = default)
    {
        var document = await _schemas
            .Find(x => x.ProjectId == projectId && x.Name == entityName)
            .SortByDescending(x => x.ScenarioVersion)
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : ToEntitySchema(document);
    }

    public async Task<IReadOnlyList<EntitySchema>> GetAllAsync(
        Guid projectId,
        int scenarioVersion,
        CancellationToken cancellationToken = default)
    {
        var documents = await _schemas
            .Find(x => x.ProjectId == projectId && x.ScenarioVersion == scenarioVersion)
            .ToListAsync(cancellationToken);

        return documents.ConvertAll(ToEntitySchema);
    }

    public async Task<IReadOnlyList<EntitySchema>> GetAllAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var documents = await _schemas
            .Find(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        return documents.ConvertAll(ToEntitySchema);
    }

    private static EntitySchema ToEntitySchema(EntitySchemaDocument document)
    {
        var schemaId = document.SchemaId == Guid.Empty ? document.Id : document.SchemaId;
        var schemaSnapshotId = document.SchemaSnapshotId == Guid.Empty ? document.Id : document.SchemaSnapshotId;

        return new EntitySchema(
            schemaSnapshotId,
            schemaId,
            document.ProjectId,
            document.ScenarioVersion,
            document.Name,
            [.. document.Fields.Select(ToEntityField)]);
    }

    private static EntityField ToEntityField(EntitySchemaFieldDocument field)
    {
        _ = Enum.TryParse<FieldType>(field.Type, ignoreCase: true, out var fieldType);

        return new EntityField(
            field.Name,
            fieldType,
            field.IsRequired,
            field.DefaultValue,
            field.EnumValues,
            field.ReferenceEntityName);
    }
}
