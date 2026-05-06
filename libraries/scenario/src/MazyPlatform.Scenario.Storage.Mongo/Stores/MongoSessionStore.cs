namespace MazyPlatform.Scenario.Storage.Mongo.Stores;

using MazyPlatform.Scenario.Abstractions.Sessions;
using MazyPlatform.Scenario.Storage.Mongo.Documents;
using MazyPlatform.Scenario.Storage.Mongo.Serialization;

using MongoDB.Bson;
using MongoDB.Driver;

internal sealed class MongoSessionStore(IMongoDatabase database) : ISessionStore
{
    private readonly IMongoCollection<SessionDocument> _sessions = database.GetCollection<SessionDocument>("sessions");

    public async Task<ISession> GetOrCreateAsync(
        Guid botId,
        string platformUserId,
        Guid startNodeId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<SessionDocument>.Filter.And(
            Builders<SessionDocument>.Filter.Eq(x => x.BotId, botId),
            Builders<SessionDocument>.Filter.Eq(x => x.PlatformUserId, platformUserId),
            Builders<SessionDocument>.Filter.Ne(x => x.State, nameof(SessionState.Completed)));

        var update = Builders<SessionDocument>.Update
            .SetOnInsert(x => x.Id, Guid.NewGuid())
            .SetOnInsert(x => x.BotId, botId)
            .SetOnInsert(x => x.PlatformUserId, platformUserId)
            .SetOnInsert(x => x.CurrentNodeId, startNodeId)
            .SetOnInsert(x => x.State, nameof(SessionState.Active))
            .SetOnInsert(x => x.Variables, [])
            .SetOnInsert(x => x.CreatedAt, now)
            .Set(x => x.UpdatedAt, now);

        var options = new FindOneAndUpdateOptions<SessionDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        var document = await _sessions.FindOneAndUpdateAsync(
            filter,
            update,
            options,
            cancellationToken);

        return ToSession(document);
    }

    public async Task SaveAsync(ISession session, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(session);
        document.UpdatedAt = DateTime.UtcNow;

        await _sessions.ReplaceOneAsync(
            x => x.Id == session.SessionId,
            document,
            cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _sessions.DeleteOneAsync(x => x.Id == sessionId, cancellationToken);
    }

    private static MongoSession ToSession(SessionDocument document)
    {
        return new MongoSession
        {
            SessionId = document.Id,
            BotId = document.BotId,
            PlatformUserId = document.PlatformUserId,
            CurrentNodeId = document.CurrentNodeId,
            State = ParseState(document.State),
            Variables = BsonValueConverter.ToDictionary(document.Variables),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
        };
    }

    private static SessionDocument ToDocument(ISession session)
    {
        return new SessionDocument
        {
            Id = session.SessionId,
            BotId = session.BotId,
            PlatformUserId = session.PlatformUserId,
            CurrentNodeId = session.CurrentNodeId,
            State = session.State.ToString(),
            Variables = BsonValueConverter.ToBsonDocument(session.Variables),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
        };
    }

    private static SessionState ParseState(string state)
    {
        return Enum.TryParse<SessionState>(state, ignoreCase: true, out var result)
            ? result
            : SessionState.Active;
    }
}
