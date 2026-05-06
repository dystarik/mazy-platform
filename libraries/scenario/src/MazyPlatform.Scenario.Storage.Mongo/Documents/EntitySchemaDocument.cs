namespace MazyPlatform.Scenario.Storage.Mongo.Documents;

using MongoDB.Bson.Serialization.Attributes;

internal sealed class EntitySchemaDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid SchemaSnapshotId { get; set; }

    public Guid SchemaId { get; set; }

    public Guid ProjectId { get; set; }

    public int ScenarioVersion { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<EntitySchemaFieldDocument> Fields { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
