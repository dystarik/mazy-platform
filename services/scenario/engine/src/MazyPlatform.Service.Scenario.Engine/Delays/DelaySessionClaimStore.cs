namespace MazyPlatform.Service.Scenario.Engine.Delays;

using System.Globalization;

using MazyPlatform.Scenario.Abstractions.Sessions;

using MongoDB.Bson;
using MongoDB.Driver;

internal sealed class DelaySessionClaimStore(IMongoDatabase database)
{
    private const string _resumeAtKey = "__delay_resume_at";
    private const string _botIdField = "botId";
    private const string _createdAtField = "createdAt";
    private const string _currentNodeIdField = "currentNodeId";
    private const string _lockUntilField = "delayResumeLockUntil";
    private const string _lockedAtField = "delayResumeLockedAt";
    private const string _platformUserIdField = "platformUserId";
    private const string _resumeAtPath = _variablesField + "." + _resumeAtKey;
    private const string _stateField = "state";
    private const string _updatedAtField = "updatedAt";
    private const string _variablesField = "variables";

    private readonly IMongoCollection<BsonDocument> _sessions = database.GetCollection<BsonDocument>("sessions");

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var index = CreateDelayResumeIndex();

        try
        {
            await _sessions.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (IsIndexConflict(ex))
        {
            await TryDropDelayResumeIndexAsync(cancellationToken);
            await _sessions.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ISession>> ClaimDueAsync(
        DateTimeOffset now,
        int batchSize,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        var sessions = new List<ISession>(batchSize);

        for (var i = 0; i < batchSize; i++)
        {
            var document = await ClaimOneAsync(now, lockDuration, cancellationToken);

            if (document is null)
                break;

            sessions.Add(ToSession(document));
        }

        return sessions;
    }

    private static CreateIndexModel<BsonDocument> CreateDelayResumeIndex()
    {
        return new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys
                .Ascending(_stateField)
                .Ascending(_resumeAtPath)
                .Ascending(_lockUntilField),
            new CreateIndexOptions
            {
                Name = "ix_sessions_delay_resume",
                Sparse = true,
            });
    }

    private static bool IsIndexConflict(MongoCommandException ex)
    {
        return ex.Code is 85 or 86
            || string.Equals(ex.CodeName, "IndexOptionsConflict", StringComparison.Ordinal)
            || string.Equals(ex.CodeName, "IndexKeySpecsConflict", StringComparison.Ordinal);
    }

    private static bool IsMissingIndexOrCollection(MongoCommandException ex)
    {
        return ex.Code == 26
            || string.Equals(ex.CodeName, "IndexNotFound", StringComparison.Ordinal)
            || string.Equals(ex.CodeName, "NamespaceNotFound", StringComparison.Ordinal);
    }

    private static DelaySession ToSession(BsonDocument document)
    {
        return new DelaySession
        {
            SessionId = ReadGuid(document.GetValue("_id", document.GetValue("Id", BsonNull.Value))),
            BotId = ReadGuid(document.GetValue(_botIdField)),
            PlatformUserId = document.GetValue(_platformUserIdField, string.Empty).AsString,
            CurrentNodeId = ReadOptionalGuid(document.GetValue(_currentNodeIdField, BsonNull.Value)),
            State = ParseState(document.GetValue(_stateField, nameof(SessionState.Active)).AsString),
            Variables = ToDictionary(document.GetValue(_variablesField, new BsonDocument()).AsBsonDocument),
            CreatedAt = ReadDateTime(document.GetValue(_createdAtField, BsonNull.Value)),
            UpdatedAt = ReadDateTime(document.GetValue(_updatedAtField, BsonNull.Value)),
        };
    }

    private static SessionState ParseState(string state)
    {
        return Enum.TryParse<SessionState>(state, ignoreCase: true, out var result)
            ? result
            : SessionState.Active;
    }

    private static Guid? ReadOptionalGuid(BsonValue value)
    {
        return value.IsBsonNull ? null : ReadGuid(value);
    }

    private static Guid ReadGuid(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.String => Guid.Parse(value.AsString),
            BsonType.Binary when value.IsGuid => value.AsGuid,
            _ => Guid.Parse(value.ToString() ?? string.Empty),
        };
    }

    private static DateTime ReadDateTime(BsonValue value)
    {
        return value.IsBsonNull ? DateTime.UtcNow : value.ToUniversalTime();
    }

    private static Dictionary<string, object?> ToDictionary(BsonDocument source)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var element in source.Elements)
        {
            dictionary[element.Name] = ToObjectValue(element.Value);
        }

        return dictionary;
    }

    private static object? ToObjectValue(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Document => ToDictionary(value.AsBsonDocument),
            BsonType.Array => value.AsBsonArray.Select(ToObjectValue).ToList(),
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Decimal128 => Decimal128.ToDecimal(value.AsDecimal128),
            BsonType.Double => value.AsDouble,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Null => null,
            BsonType.String => value.AsString,
            BsonType.Binary when value.IsGuid => value.AsGuid,
            _ => value.ToString(),
        };
    }

    private async Task TryDropDelayResumeIndexAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _sessions.Indexes.DropOneAsync("ix_sessions_delay_resume", cancellationToken);
        }
        catch (MongoCommandException ex) when (IsMissingIndexOrCollection(ex))
        {
            _ = ex;
        }
    }

    private async Task<BsonDocument?> ClaimOneAsync(
        DateTimeOffset now,
        TimeSpan lockDuration,
        CancellationToken cancellationToken)
    {
        var nowUtc = now.UtcDateTime;
        var dueUntil = now.ToString("O", CultureInfo.InvariantCulture);
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq(_stateField, nameof(SessionState.WaitingForEvent)),
            Builders<BsonDocument>.Filter.Ne(_currentNodeIdField, BsonNull.Value),
            Builders<BsonDocument>.Filter.Exists(_resumeAtPath),
            Builders<BsonDocument>.Filter.Lte(_resumeAtPath, dueUntil),
            Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Exists(_lockUntilField, false),
                Builders<BsonDocument>.Filter.Lte(_lockUntilField, nowUtc)));
        var update = Builders<BsonDocument>.Update
            .Set(_lockUntilField, nowUtc.Add(lockDuration))
            .Set(_lockedAtField, nowUtc);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Sort = Builders<BsonDocument>.Sort.Ascending(_resumeAtPath),
        };

        return await _sessions.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }
}
