namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.RuntimeData;

using System.Globalization;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

internal sealed class MongoRuntimeUserDataReader : IRuntimeUserDataReader
{
    private const string EntityRecordsCollectionName = "entity_records";
    private const string EntitySchemasCollectionName = "entity_schemas";

    private static readonly JsonWriterSettings JsonWriterSettings = new()
    {
        OutputMode = JsonOutputMode.RelaxedExtendedJson,
    };

    private readonly IMongoCollection<BsonDocument> _records;
    private readonly IMongoCollection<BsonDocument> _schemas;

    public MongoRuntimeUserDataReader(IOptions<RuntimeMongoOptions> options)
    {
        var mongoOptions = options.Value;
        var client = new MongoClient(mongoOptions.ConnectionString);
        var database = client.GetDatabase(mongoOptions.DatabaseName);
        _records = database.GetCollection<BsonDocument>(EntityRecordsCollectionName);
        _schemas = database.GetCollection<BsonDocument>(EntitySchemasCollectionName);
    }

    public async Task<RuntimeUserDataReadResult> GetRecordsAsync(
        RuntimeUserDataReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(request);
        var totalCount = (int)await _records.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var records = await _records
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("updatedAt"))
            .Skip(request.PageOffset)
            .Limit(request.PageSize)
            .ToListAsync(cancellationToken);

        var schemaNames = await LoadSchemaNamesAsync(records, cancellationToken);
        var items = records
            .Select(record => ToResult(record, schemaNames))
            .ToList();

        return new RuntimeUserDataReadResult(items, totalCount);
    }

    private static FilterDefinition<BsonDocument> BuildFilter(RuntimeUserDataReadRequest request)
    {
        var builder = Builders<BsonDocument>.Filter;
        var filters = new List<FilterDefinition<BsonDocument>>
        {
            builder.Eq("projectId", ToBsonGuid(request.ProjectId)),
        };

        if (!request.IncludeArchived)
        {
            filters.Add(builder.Eq("isArchived", false));
        }

        if (request.SchemaId is { } schemaId)
        {
            filters.Add(builder.Eq("schemaId", ToBsonGuid(schemaId)));
        }

        if (request.ScenarioVersion is { } scenarioVersion)
        {
            filters.Add(builder.Eq("scenarioVersion", scenarioVersion));
        }

        if (request.BotId is { } botId)
        {
            filters.Add(builder.Eq("botId", ToBsonGuid(botId)));
        }

        if (!string.IsNullOrWhiteSpace(request.PlatformUserId))
        {
            filters.Add(builder.Eq("platformUserId", request.PlatformUserId));
        }

        return builder.And(filters);
    }

    private static BsonBinaryData ToBsonGuid(Guid value) => new(value, GuidRepresentation.Standard);

    private static RuntimeUserDataRecord ToResult(
        BsonDocument record,
        IReadOnlyDictionary<Guid, string> schemaNames)
    {
        var schemaSnapshotId = GetGuid(record, "schemaSnapshotId");
        var schemaName = schemaNames.TryGetValue(schemaSnapshotId, out var name)
            ? name
            : string.Empty;

        return new RuntimeUserDataRecord(
            GetGuid(record, "_id"),
            GetGuid(record, "schemaId"),
            schemaSnapshotId,
            schemaName,
            GetInt32(record, "scenarioVersion"),
            GetGuid(record, "botId"),
            GetString(record, "platformUserId"),
            GetNullableGuid(record, "sessionId"),
            GetBoolean(record, "isArchived"),
            GetDocument(record, "data").ToJson(JsonWriterSettings),
            GetDateTimeOffset(record, "createdAt"),
            GetDateTimeOffset(record, "updatedAt"));
    }

    private static Guid GetGuid(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return Guid.Empty;
        }

        return value switch
        {
            BsonBinaryData binary => binary.ToGuid(GuidRepresentation.Standard),
            BsonString text when Guid.TryParse(text.Value, out var guid) => guid,
            _ => Guid.Empty,
        };
    }

    private static Guid? GetNullableGuid(BsonDocument document, string fieldName)
    {
        var value = GetGuid(document, fieldName);
        return value == Guid.Empty ? null : value;
    }

    private static string GetString(BsonDocument document, string fieldName)
    {
        return document.TryGetValue(fieldName, out var value) && value.IsString
            ? value.AsString
            : string.Empty;
    }

    private static int GetInt32(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value))
        {
            return 0;
        }

        return value switch
        {
            { IsInt32: true } => value.AsInt32,
            { IsInt64: true } => checked((int)value.AsInt64),
            { IsString: true } when int.TryParse(value.AsString, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0,
        };
    }

    private static bool GetBoolean(BsonDocument document, string fieldName)
    {
        return document.TryGetValue(fieldName, out var value) && value.IsBoolean && value.AsBoolean;
    }

    private static BsonDocument GetDocument(BsonDocument document, string fieldName)
    {
        return document.TryGetValue(fieldName, out var value) && value.IsBsonDocument
            ? value.AsBsonDocument
            : [];
    }

    private static DateTimeOffset GetDateTimeOffset(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return DateTimeOffset.UnixEpoch;
        }

        if (value.IsBsonDateTime)
        {
            return new DateTimeOffset(value.ToUniversalTime());
        }

        return value.IsString && DateTimeOffset.TryParse(value.AsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : DateTimeOffset.UnixEpoch;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadSchemaNamesAsync(
        IReadOnlyCollection<BsonDocument> records,
        CancellationToken cancellationToken)
    {
        var snapshotIds = records
            .Select(record => GetGuid(record, "schemaSnapshotId"))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (snapshotIds.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var schemas = await _schemas
            .Find(Builders<BsonDocument>.Filter.In("_id", snapshotIds.Select(ToBsonGuid)))
            .ToListAsync(cancellationToken);

        return schemas
            .Select(schema => new
            {
                Id = GetGuid(schema, "_id"),
                Name = GetString(schema, "name"),
            })
            .Where(schema => schema.Id != Guid.Empty)
            .ToDictionary(schema => schema.Id, schema => schema.Name);
    }
}
