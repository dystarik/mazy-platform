namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using MongoDB.Bson;
using MongoDB.Driver;

internal sealed class MongoRuntimeDataSeeder
{
    private readonly IMongoDatabase _database;

    public MongoRuntimeDataSeeder(ScenarioRepositoryFixture app)
    {
        var client = new MongoClient(app.MongoHostConnectionString);
        _database = client.GetDatabase(ScenarioRepositoryFixture.MongoDatabase);
    }

    public async Task SeedRecordAsync(
        Guid projectId,
        Guid schemaId,
        Guid schemaSnapshotId,
        int scenarioVersion,
        Guid botId,
        string platformUserId,
        bool isArchived,
        string name)
    {
        var now = DateTime.UtcNow;
        var schemas = _database.GetCollection<BsonDocument>("entity_schemas");
        var records = _database.GetCollection<BsonDocument>("entity_records");

        await schemas.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", ToBsonGuid(schemaSnapshotId)),
            Builders<BsonDocument>.Update
                .SetOnInsert("_id", ToBsonGuid(schemaSnapshotId))
                .Set("name", name),
            new UpdateOptions { IsUpsert = true });

        await records.InsertOneAsync(new BsonDocument
        {
            ["_id"] = ToBsonGuid(Guid.NewGuid()),
            ["projectId"] = ToBsonGuid(projectId),
            ["schemaId"] = ToBsonGuid(schemaId),
            ["schemaSnapshotId"] = ToBsonGuid(schemaSnapshotId),
            ["scenarioVersion"] = scenarioVersion,
            ["botId"] = ToBsonGuid(botId),
            ["platformUserId"] = platformUserId,
            ["sessionId"] = BsonNull.Value,
            ["isArchived"] = isArchived,
            ["data"] = new BsonDocument { ["name"] = name },
            ["createdAt"] = now,
            ["updatedAt"] = now,
        });
    }

    private static BsonBinaryData ToBsonGuid(Guid value) => new(value, GuidRepresentation.Standard);
}
