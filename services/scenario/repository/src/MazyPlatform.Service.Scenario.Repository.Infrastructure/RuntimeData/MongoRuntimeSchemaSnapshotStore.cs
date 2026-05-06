namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.RuntimeData;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;

using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Driver;

internal sealed class MongoRuntimeSchemaSnapshotStore : IRuntimeSchemaSnapshotStore
{
    private const string EntitySchemasCollectionName = "entity_schemas";
    private const string RuntimeSchemaIndexName = "ix_entity_schemas_project_version_name";

    private static readonly string[] LegacyEntitySchemaIndexNames =
    [
        "projectId_1_name_1",
        "ProjectId_1_Name_1",
    ];

    private readonly IMongoCollection<BsonDocument> _schemas;

    public MongoRuntimeSchemaSnapshotStore(IOptions<RuntimeMongoOptions> options)
    {
        var mongoOptions = options.Value;
        var client = new MongoClient(mongoOptions.ConnectionString);
        var database = client.GetDatabase(mongoOptions.DatabaseName);
        _schemas = database.GetCollection<BsonDocument>(EntitySchemasCollectionName);
    }

    public async Task UpsertProjectSchemasAsync(
        Guid projectId,
        int scenarioVersion,
        IReadOnlyCollection<EntitySchema> schemas,
        CancellationToken cancellationToken = default)
    {
        await EnsureIndexesAsync(cancellationToken);

        foreach (var schema in schemas)
        {
            await UpsertSchemaAsync(projectId, scenarioVersion, schema, cancellationToken);
        }
    }

    private static BsonBinaryData ToBsonGuid(Guid value) => new(value, GuidRepresentation.Standard);

    private static BsonDocument ToFieldDocument(EntityField field)
    {
        var document = new BsonDocument
        {
            ["name"] = field.Name,
            ["type"] = field.FieldType.ToString(),
            ["isRequired"] = field.IsRequired,
            ["defaultValue"] = field.DefaultValue is null ? BsonNull.Value : field.DefaultValue,
            ["enumValues"] = BsonNull.Value,
            ["referenceEntityName"] = BsonNull.Value,
        };

        return document;
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        foreach (var indexName in LegacyEntitySchemaIndexNames)
        {
            try
            {
                await _schemas.Indexes.DropOneAsync(indexName, cancellationToken);
            }
            catch (MongoCommandException ex)
                when (string.Equals(ex.CodeName, "IndexNotFound", StringComparison.Ordinal)
                    || string.Equals(ex.CodeName, "NamespaceNotFound", StringComparison.Ordinal)
                    || ex.Code == 26)
            {
                _ = ex;
            }
        }

        var keys = Builders<BsonDocument>.IndexKeys
            .Ascending("projectId")
            .Ascending("scenarioVersion")
            .Ascending("name");

        var index = new CreateIndexModel<BsonDocument>(
            keys,
            new CreateIndexOptions
            {
                Name = RuntimeSchemaIndexName,
                Unique = true,
            });

        await _schemas.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
    }

    private async Task UpsertSchemaAsync(
        Guid projectId,
        int scenarioVersion,
        EntitySchema schema,
        CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("projectId", ToBsonGuid(projectId)),
            Builders<BsonDocument>.Filter.Eq("scenarioVersion", scenarioVersion),
            Builders<BsonDocument>.Filter.Eq("name", schema.Name));

        var existing = await _schemas.Find(filter).FirstOrDefaultAsync(cancellationToken);
        var snapshotId = existing?.GetValue("_id").AsGuid ?? Guid.NewGuid();
        var now = DateTime.UtcNow;

        var document = new BsonDocument
        {
            ["_id"] = ToBsonGuid(snapshotId),
            ["schemaSnapshotId"] = ToBsonGuid(snapshotId),
            ["schemaId"] = ToBsonGuid(schema.Id),
            ["projectId"] = ToBsonGuid(projectId),
            ["scenarioVersion"] = scenarioVersion,
            ["name"] = schema.Name,
            ["fields"] = new BsonArray(schema.Fields.Select(ToFieldDocument)),
            ["createdAt"] = existing?.GetValue("createdAt", now) ?? now,
            ["updatedAt"] = now,
        };

        await _schemas.ReplaceOneAsync(
            filter,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}
