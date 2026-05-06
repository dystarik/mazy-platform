namespace MazyPlatform.Scenario.Storage.Mongo.Stores;

using MazyPlatform.Scenario.Abstractions.Sessions;

internal sealed class MongoSession : ISession
{
    public Guid SessionId { get; init; }

    public Guid BotId { get; init; }

    public string PlatformUserId { get; init; } = string.Empty;

    public Guid? CurrentNodeId { get; set; }

    public SessionState State { get; set; }

    public IDictionary<string, object?> Variables { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }
}
