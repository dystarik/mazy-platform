namespace MazyPlatform.Scenario.Storage.Mongo.Documents;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

internal sealed class SessionDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid BotId { get; set; }

    public string PlatformUserId { get; set; } = string.Empty;

    public Guid? CurrentNodeId { get; set; }

    public string State { get; set; } = string.Empty;

    public BsonDocument Variables { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
