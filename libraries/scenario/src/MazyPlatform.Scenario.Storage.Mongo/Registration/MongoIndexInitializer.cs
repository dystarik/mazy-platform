namespace MazyPlatform.Scenario.Storage.Mongo.Registration;

using System.Globalization;

using MazyPlatform.Scenario.Storage.Mongo.Configuration;
using MazyPlatform.Scenario.Storage.Mongo.Documents;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Driver;

internal sealed class MongoIndexInitializer(IMongoDatabase database, IOptions<MongoStorageOptions> options) : IHostedService
{
    private readonly IMongoCollection<SessionDocument> _sessions = database.GetCollection<SessionDocument>("sessions");
    private readonly IMongoCollection<EntityRecordDocument> _entityRecords = database.GetCollection<EntityRecordDocument>("entity_records");
    private readonly IMongoCollection<EntitySchemaDocument> _entitySchemas = database.GetCollection<EntitySchemaDocument>("entity_schemas");
    private readonly IMongoCollection<BsonDocument> _rawSessions = database.GetCollection<BsonDocument>("sessions");
    private readonly IMongoCollection<BsonDocument> _rawEntityRecords = database.GetCollection<BsonDocument>("entity_records");
    private readonly IMongoCollection<BsonDocument> _rawEntitySchemas = database.GetCollection<BsonDocument>("entity_schemas");
    private readonly MongoStorageOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureLegacyEntitySchemasAsync(cancellationToken);
        await EnsureLegacyEntityRecordsAsync(cancellationToken);
        await EnsureSessionIndexesAsync(cancellationToken);
        await EnsureEntityRecordIndexesAsync(cancellationToken);
        await EnsureEntitySchemaIndexesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureSessionIndexesAsync(CancellationToken cancellationToken)
    {
        await TryDropLegacyUniqueSessionIndexAsync(cancellationToken);

        var botUserIndex = new CreateIndexModel<SessionDocument>(
            Builders<SessionDocument>.IndexKeys
                .Ascending(x => x.BotId)
                .Ascending(x => x.PlatformUserId),
            new CreateIndexOptions
            {
                Name = "ix_sessions_botid_platformuserid",
            });

        var ttlIndex = new CreateIndexModel<SessionDocument>(
            Builders<SessionDocument>.IndexKeys.Ascending(x => x.UpdatedAt),
            new CreateIndexOptions
            {
                Name = "ix_sessions_updatedat_ttl",
                ExpireAfter = TimeSpan.FromHours(_options.SessionTtlHours),
            });

        await _sessions.Indexes.CreateManyAsync([botUserIndex, ttlIndex], cancellationToken);
    }

    private async Task TryDropLegacyUniqueSessionIndexAsync(CancellationToken cancellationToken)
    {
        const string legacyIndexName = "BotId_1_PlatformUserId_1";

        try
        {
            await _sessions.Indexes.DropOneAsync(legacyIndexName, cancellationToken);
        }
        catch (MongoCommandException ex)
            when (string.Equals(ex.CodeName, "IndexNotFound", StringComparison.Ordinal)
                || string.Equals(ex.CodeName, "NamespaceNotFound", StringComparison.Ordinal)
                || ex.Code == 26)
        {
            _ = ex;
        }
    }

    private async Task EnsureEntityRecordIndexesAsync(CancellationToken cancellationToken)
    {
        await TryDropLegacyEntityRecordIndexesAsync(cancellationToken);

        var schemaIdIndex = new CreateIndexModel<EntityRecordDocument>(
            Builders<EntityRecordDocument>.IndexKeys.Ascending(x => x.SchemaId),
            new CreateIndexOptions { Name = "ix_entity_records_schemaid" });

        var sessionIdIndex = new CreateIndexModel<EntityRecordDocument>(
            Builders<EntityRecordDocument>.IndexKeys.Ascending(x => x.SessionId),
            new CreateIndexOptions { Name = "ix_entity_records_sessionid", Sparse = true });

        var projectIdIndex = new CreateIndexModel<EntityRecordDocument>(
            Builders<EntityRecordDocument>.IndexKeys.Ascending(x => x.ProjectId),
            new CreateIndexOptions { Name = "ix_entity_records_projectid" });

        var userScopeIndex = new CreateIndexModel<EntityRecordDocument>(
            Builders<EntityRecordDocument>.IndexKeys
                .Ascending(x => x.ProjectId)
                .Ascending(x => x.SchemaId)
                .Ascending(x => x.BotId)
                .Ascending(x => x.PlatformUserId)
                .Ascending(x => x.IsArchived),
            new CreateIndexOptions { Name = "ix_entity_records_user_scope" });

        await _entityRecords.Indexes.CreateManyAsync(
            [schemaIdIndex, sessionIdIndex, projectIdIndex, userScopeIndex],
            cancellationToken);
    }

    private async Task TryDropLegacyEntityRecordIndexesAsync(CancellationToken cancellationToken)
    {
        foreach (var indexName in new[] { "schemaId_1", "sessionId_1", "projectId_1", "SchemaId_1", "SessionId_1", "ProjectId_1" })
        {
            try
            {
                await _entityRecords.Indexes.DropOneAsync(indexName, cancellationToken);
            }
            catch (MongoCommandException ex)
                when (string.Equals(ex.CodeName, "IndexNotFound", StringComparison.Ordinal)
                    || string.Equals(ex.CodeName, "NamespaceNotFound", StringComparison.Ordinal)
                    || ex.Code == 26)
            {
                _ = ex;
            }
        }
    }

    private async Task EnsureEntitySchemaIndexesAsync(CancellationToken cancellationToken)
    {
        await TryDropLegacyEntitySchemaIndexesAsync(cancellationToken);

        var projectNameUniqueIndex = new CreateIndexModel<EntitySchemaDocument>(
            Builders<EntitySchemaDocument>.IndexKeys
                .Ascending(x => x.ProjectId)
                .Ascending(x => x.ScenarioVersion)
                .Ascending(x => x.Name),
            new CreateIndexOptions
            {
                Name = "ix_entity_schemas_project_version_name",
                Unique = true,
            });

        await _entitySchemas.Indexes.CreateOneAsync(projectNameUniqueIndex, cancellationToken: cancellationToken);
    }

    private async Task TryDropLegacyEntitySchemaIndexesAsync(CancellationToken cancellationToken)
    {
        foreach (var indexName in new[] { "projectId_1_name_1", "ProjectId_1_Name_1" })
        {
            try
            {
                await _entitySchemas.Indexes.DropOneAsync(indexName, cancellationToken);
            }
            catch (MongoCommandException ex)
                when (string.Equals(ex.CodeName, "IndexNotFound", StringComparison.Ordinal)
                    || string.Equals(ex.CodeName, "NamespaceNotFound", StringComparison.Ordinal)
                    || ex.Code == 26)
            {
                _ = ex;
            }
        }
    }

    private async Task EnsureLegacyEntitySchemasAsync(CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("schemaId", false),
            Builders<BsonDocument>.Filter.Exists("schemaSnapshotId", false),
            Builders<BsonDocument>.Filter.Exists("scenarioVersion", false));

        var schemas = await _rawEntitySchemas.Find(filter).ToListAsync(cancellationToken);

        foreach (var schema in schemas)
        {
            var id = schema.GetValue("_id");
            var update = Builders<BsonDocument>.Update
                .Set("schemaId", schema.GetValue("schemaId", id))
                .Set("schemaSnapshotId", schema.GetValue("schemaSnapshotId", id))
                .Set("scenarioVersion", schema.GetValue("scenarioVersion", 0));

            await _rawEntitySchemas.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                update,
                cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureLegacyEntityRecordsAsync(CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("schemaSnapshotId", false),
            Builders<BsonDocument>.Filter.Exists("scenarioVersion", false),
            Builders<BsonDocument>.Filter.Exists("botId", false),
            Builders<BsonDocument>.Filter.Exists("platformUserId", false),
            Builders<BsonDocument>.Filter.Exists("isArchived", false));

        var records = await _rawEntityRecords.Find(filter).ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            var id = record.GetValue("_id");
            var schemaId = record.GetValue("schemaId", BsonNull.Value);
            var update = Builders<BsonDocument>.Update
                .Set("schemaSnapshotId", record.GetValue("schemaSnapshotId", schemaId))
                .Set("scenarioVersion", record.GetValue("scenarioVersion", 0));

            var session = await FindSessionAsync(record.GetValue("sessionId", BsonNull.Value), cancellationToken);
            if (session is null)
            {
                update = update.Set("isArchived", true);
            }
            else
            {
                update = update
                    .Set("botId", session.GetValue("botId", BsonNull.Value))
                    .Set("platformUserId", session.GetValue("platformUserId", string.Empty))
                    .Set("isArchived", false);
            }

            await _rawEntityRecords.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                update,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<BsonDocument?> FindSessionAsync(BsonValue sessionId, CancellationToken cancellationToken)
    {
        if (sessionId.IsBsonNull)
        {
            return null;
        }

        return await _rawSessions
            .Find(Builders<BsonDocument>.Filter.Eq("_id", sessionId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
