namespace MazyPlatform.Scenario.Storage.Mongo.Documents;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

internal sealed class EntityRecordDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid SchemaId { get; set; }

    public Guid SchemaSnapshotId { get; set; }

    public Guid ProjectId { get; set; }

    public int ScenarioVersion { get; set; }

    public Guid BotId { get; set; }

    public string PlatformUserId { get; set; } = string.Empty;

    public Guid? SessionId { get; set; }

    public bool IsArchived { get; set; }

    public BsonDocument Data { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
