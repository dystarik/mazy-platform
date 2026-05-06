namespace MazyPlatform.Scenario.Storage.Mongo.Stores;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Storage.Mongo.Documents;
using MazyPlatform.Scenario.Storage.Mongo.Normalization;
using MazyPlatform.Scenario.Storage.Mongo.Serialization;

using MongoDB.Driver;

internal sealed class MongoDataStore(IMongoDatabase database) : IDataStore
{
    private readonly IMongoCollection<EntityRecordDocument> _records = database.GetCollection<EntityRecordDocument>("entity_records");
    private readonly IMongoCollection<EntitySchemaDocument> _schemas = database.GetCollection<EntitySchemaDocument>("entity_schemas");

    public async Task<EntityRecord> CreateAsync(
        Guid schemaSnapshotId,
        IReadOnlyDictionary<string, object?> data,
        DataScope scope,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var schema = await GetSchemaSnapshotByIdAsync(schemaSnapshotId, cancellationToken)
            ?? throw new InvalidOperationException($"Snapshot схемы {schemaSnapshotId} не найден.");

        var validatedData = SchemaDataNormalizer.ValidateAndNormalizeData(schema, data, requireAllFields: true);
        var schemaId = schema.SchemaId == Guid.Empty ? schema.Id : schema.SchemaId;
        var now = DateTime.UtcNow;
        var document = new EntityRecordDocument
        {
            Id = Guid.NewGuid(),
            SchemaId = schemaId,
            SchemaSnapshotId = schema.Id,
            ProjectId = scope.ProjectId,
            ScenarioVersion = scope.ScenarioVersion,
            BotId = scope.BotId,
            PlatformUserId = scope.PlatformUserId,
            SessionId = sessionId,
            Data = BsonValueConverter.ToBsonDocument(validatedData),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _records.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToEntityRecord(document);
    }

    public async Task<EntityRecord> CreateAsync(
        Guid schemaId,
        IReadOnlyDictionary<string, object?> data,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var schema = await GetSchemaSnapshotByIdAsync(schemaId, cancellationToken)
            ?? throw new InvalidOperationException($"Схема {schemaId} не найдена.");

        var validatedData = SchemaDataNormalizer.ValidateAndNormalizeData(schema, data, requireAllFields: true);
        var stableSchemaId = schema.SchemaId == Guid.Empty ? schema.Id : schema.SchemaId;
        var now = DateTime.UtcNow;
        var document = new EntityRecordDocument
        {
            Id = Guid.NewGuid(),
            SchemaId = stableSchemaId,
            SchemaSnapshotId = schema.Id,
            ProjectId = schema.ProjectId,
            ScenarioVersion = schema.ScenarioVersion,
            SessionId = sessionId,
            Data = BsonValueConverter.ToBsonDocument(validatedData),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _records.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToEntityRecord(document);
    }

    public async Task<EntityRecord?> GetAsync(
        Guid recordId,
        DataScope scope,
        CancellationToken cancellationToken = default)
    {
        var document = await _records.Find(BuildScopedRecordFilter(recordId, scope)).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToEntityRecord(document);
    }

    public async Task<EntityRecord?> GetAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var document = await _records.Find(x => x.Id == recordId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToEntityRecord(document);
    }

    public async Task UpdateAsync(
        Guid recordId,
        IReadOnlyDictionary<string, object?> data,
        DataScope scope,
        CancellationToken cancellationToken = default)
    {
        var existing = await _records.Find(BuildScopedRecordFilter(recordId, scope)).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Запись {recordId} не найдена.");

        var schema = await GetSchemaByStableIdAsync(existing.SchemaId, scope.ProjectId, scope.ScenarioVersion, cancellationToken)
            ?? throw new InvalidOperationException($"Схема {existing.SchemaId} для версии {scope.ScenarioVersion} не найдена.");

        var validatedData = SchemaDataNormalizer.ValidateAndNormalizeData(schema, data, requireAllFields: false);
        var update = Builders<EntityRecordDocument>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow);

        foreach (var (key, value) in validatedData)
        {
            update = update.Set($"data.{key}", BsonValueConverter.ToBsonValue(value));
        }

        await _records.UpdateOneAsync(BuildScopedRecordFilter(recordId, scope), update, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        Guid recordId,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default)
    {
        var existing = await _records.Find(x => x.Id == recordId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Запись {recordId} не найдена.");

        var schema = await GetSchemaByStableIdAsync(
                existing.SchemaId,
                existing.ProjectId,
                existing.ScenarioVersion,
                cancellationToken)
            ?? throw new InvalidOperationException($"Схема {existing.SchemaId} не найдена.");

        var validatedData = SchemaDataNormalizer.ValidateAndNormalizeData(schema, data, requireAllFields: true);
        var update = Builders<EntityRecordDocument>.Update
            .Set(x => x.Data, BsonValueConverter.ToBsonDocument(validatedData))
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _records.UpdateOneAsync(x => x.Id == recordId, update, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<EntityRecord>> QueryAsync(
        Guid schemaId,
        DataScope scope,
        IReadOnlyDictionary<string, object?>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var querySchemaId = schemaId;
        var normalizedFilter = filter;

        if (filter is not null)
        {
            var schema = await GetScopedQuerySchemaAsync(schemaId, scope, cancellationToken)
                ?? throw new InvalidOperationException($"Схема {schemaId} для версии {scope.ScenarioVersion} не найдена.");

            querySchemaId = schema.SchemaId == Guid.Empty ? schema.Id : schema.SchemaId;
            normalizedFilter = SchemaDataNormalizer.NormalizeFilter(schema, filter);
        }

        var filters = new List<FilterDefinition<EntityRecordDocument>>
        {
            Builders<EntityRecordDocument>.Filter.Eq(x => x.ProjectId, scope.ProjectId),
            Builders<EntityRecordDocument>.Filter.Eq(x => x.SchemaId, querySchemaId),
            Builders<EntityRecordDocument>.Filter.Eq(x => x.BotId, scope.BotId),
            Builders<EntityRecordDocument>.Filter.Eq(x => x.PlatformUserId, scope.PlatformUserId),
            Builders<EntityRecordDocument>.Filter.Ne(x => x.IsArchived, true),
        };

        AddDataFilters(filters, normalizedFilter);

        var records = await _records
            .Find(Builders<EntityRecordDocument>.Filter.And(filters))
            .ToListAsync(cancellationToken);

        return records.ConvertAll(ToEntityRecord);
    }

    public async Task<IReadOnlyList<EntityRecord>> QueryAsync(
        Guid schemaId,
        IReadOnlyDictionary<string, object?>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var querySchemaId = schemaId;
        var normalizedFilter = filter;

        if (filter is not null)
        {
            var schema = await GetLatestSchemaByStableOrSnapshotIdAsync(schemaId, cancellationToken)
                ?? throw new InvalidOperationException($"Схема {schemaId} не найдена.");

            querySchemaId = schema.SchemaId == Guid.Empty ? schema.Id : schema.SchemaId;
            normalizedFilter = SchemaDataNormalizer.NormalizeFilter(schema, filter);
        }

        var filters = new List<FilterDefinition<EntityRecordDocument>>
        {
            Builders<EntityRecordDocument>.Filter.Eq(x => x.SchemaId, querySchemaId),
        };

        AddDataFilters(filters, normalizedFilter);

        var records = await _records
            .Find(Builders<EntityRecordDocument>.Filter.And(filters))
            .ToListAsync(cancellationToken);

        return records.ConvertAll(ToEntityRecord);
    }

    public Task<EntityRecord?> GetReferencedAsync(
        Guid referenceRecordId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync(referenceRecordId, cancellationToken);
    }

    public Task DeleteAsync(Guid recordId, DataScope scope, CancellationToken cancellationToken = default)
    {
        return _records.DeleteOneAsync(BuildScopedRecordFilter(recordId, scope), cancellationToken);
    }

    public Task DeleteAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        return _records.DeleteOneAsync(x => x.Id == recordId, cancellationToken);
    }

    private static void AddDataFilters(
        ICollection<FilterDefinition<EntityRecordDocument>> filters,
        IReadOnlyDictionary<string, object?>? filter)
    {
        if (filter is null)
        {
            return;
        }

        foreach (var item in filter)
        {
            filters.Add(Builders<EntityRecordDocument>.Filter.Eq(
                $"data.{item.Key}",
                BsonValueConverter.ToBsonValue(item.Value)));
        }
    }

    private static FilterDefinition<EntityRecordDocument> BuildScopedRecordFilter(Guid recordId, DataScope scope)
    {
        return Builders<EntityRecordDocument>.Filter.And(
            Builders<EntityRecordDocument>.Filter.Eq(x => x.Id, recordId),
            Builders<EntityRecordDocument>.Filter.Eq(x => x.ProjectId, scope.ProjectId),
            Builders<EntityRecordDocument>.Filter.Eq(x => x.BotId, scope.BotId),
            Builders<EntityRecordDocument>.Filter.Eq(x => x.PlatformUserId, scope.PlatformUserId),
            Builders<EntityRecordDocument>.Filter.Ne(x => x.IsArchived, true));
    }

    private async Task<EntitySchemaDocument?> GetSchemaSnapshotByIdAsync(Guid schemaSnapshotId, CancellationToken cancellationToken)
    {
        return await _schemas.Find(x => x.Id == schemaSnapshotId).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<EntitySchemaDocument?> GetScopedQuerySchemaAsync(
        Guid schemaId,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        return await _schemas
            .Find(x => x.ProjectId == scope.ProjectId
                && x.ScenarioVersion == scope.ScenarioVersion
                && (x.SchemaId == schemaId || x.Id == schemaId || x.SchemaSnapshotId == schemaId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<EntitySchemaDocument?> GetSchemaByStableIdAsync(
        Guid schemaId,
        Guid projectId,
        int scenarioVersion,
        CancellationToken cancellationToken)
    {
        return await _schemas
            .Find(x => x.ProjectId == projectId
                && x.ScenarioVersion == scenarioVersion
                && x.SchemaId == schemaId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<EntitySchemaDocument?> GetLatestSchemaByStableOrSnapshotIdAsync(
        Guid schemaId,
        CancellationToken cancellationToken)
    {
        var stableSchema = await _schemas
            .Find(x => x.SchemaId == schemaId)
            .SortByDescending(x => x.ScenarioVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (stableSchema is not null)
        {
            return stableSchema;
        }

        return await _schemas
            .Find(x => x.Id == schemaId || x.SchemaSnapshotId == schemaId)
            .SortByDescending(x => x.ScenarioVersion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private EntityRecord ToEntityRecord(EntityRecordDocument document)
    {
        return new EntityRecord(
            document.Id,
            document.SchemaId,
            BsonValueConverter.ToDictionary(document.Data),
            document.SessionId,
            document.CreatedAt,
            document.UpdatedAt)
        {
            SchemaSnapshotId = document.SchemaSnapshotId == Guid.Empty ? document.SchemaId : document.SchemaSnapshotId,
            ProjectId = document.ProjectId,
            ScenarioVersion = document.ScenarioVersion,
            BotId = document.BotId,
            PlatformUserId = document.PlatformUserId,
            IsArchived = document.IsArchived,
        };
    }
}
