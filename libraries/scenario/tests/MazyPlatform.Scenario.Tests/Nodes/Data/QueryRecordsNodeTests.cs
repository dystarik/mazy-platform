namespace MazyPlatform.Scenario.Tests.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Data;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class QueryRecordsNodeTests
{
    [Test]
    public async Task ExecuteAsync_WithCountVariable_SavesCountAsString()
    {
        var schemaId = Guid.NewGuid();
        var schema = new EntitySchema(schemaId, Guid.NewGuid(), "Заявка", []);
        var records = new[]
        {
            new EntityRecord(Guid.NewGuid(), schemaId, new Dictionary<string, object?>(StringComparer.Ordinal)),
            new EntityRecord(Guid.NewGuid(), schemaId, new Dictionary<string, object?>(StringComparer.Ordinal)),
            new EntityRecord(Guid.NewGuid(), schemaId, new Dictionary<string, object?>(StringComparer.Ordinal)),
        };

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();
        schemaStore.GetAsync(Arg.Any<Guid>(), Arg.Any<int>(), "Заявка", Arg.Any<CancellationToken>()).Returns(schema);
        dataStore.QueryAsync(schemaId, Arg.Any<DataScope>(), Arg.Any<IReadOnlyDictionary<string, object?>?>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var node = new QueryRecordsNode(
            Guid.NewGuid(),
            "Заявка",
            "records",
            filterMappings: null,
            countVariable: "appointmentsCount");
        var context = CreateContext(dataStore, schemaStore);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["appointmentsCount"]).IsEqualTo("3");
    }

    [Test]
    public async Task ExecuteAsync_WithoutCountVariable_DoesNotCreateCountKey()
    {
        var schemaId = Guid.NewGuid();
        var schema = new EntitySchema(schemaId, Guid.NewGuid(), "Заявка", []);

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();
        schemaStore.GetAsync(Arg.Any<Guid>(), Arg.Any<int>(), "Заявка", Arg.Any<CancellationToken>()).Returns(schema);
        dataStore.QueryAsync(schemaId, Arg.Any<DataScope>(), Arg.Any<IReadOnlyDictionary<string, object?>?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityRecord>());

        var node = new QueryRecordsNode(Guid.NewGuid(), "Заявка", "records");
        var context = CreateContext(dataStore, schemaStore);

        await node.ExecuteAsync(context);

        var nonRecordsKeys = context.Session.Variables.Keys
            .Where(k => !string.Equals(k, "records", StringComparison.Ordinal))
            .ToList();
        await Assert.That(nonRecordsKeys.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_EmptyResultWithCountVariable_SavesZero()
    {
        var schemaId = Guid.NewGuid();
        var schema = new EntitySchema(schemaId, Guid.NewGuid(), "Заявка", []);

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();
        schemaStore.GetAsync(Arg.Any<Guid>(), Arg.Any<int>(), "Заявка", Arg.Any<CancellationToken>()).Returns(schema);
        dataStore.QueryAsync(schemaId, Arg.Any<DataScope>(), Arg.Any<IReadOnlyDictionary<string, object?>?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityRecord>());

        var node = new QueryRecordsNode(
            Guid.NewGuid(),
            "Заявка",
            "records",
            filterMappings: null,
            countVariable: "count");
        var context = CreateContext(dataStore, schemaStore);

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables["count"]).IsEqualTo("0");
    }

    [Test]
    public async Task ExecuteAsync_QueriesByStableSchemaIdAndUserScope()
    {
        var projectId = Guid.NewGuid();
        var schemaSnapshotId = Guid.NewGuid();
        var stableSchemaId = Guid.NewGuid();
        var schema = new EntitySchema(schemaSnapshotId, stableSchemaId, projectId, 4, "Заявка", []);

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();
        schemaStore.GetAsync(projectId, 4, "Заявка", Arg.Any<CancellationToken>()).Returns(schema);

        Guid? capturedSchemaId = null;
        DataScope? capturedScope = null;
        dataStore.QueryAsync(
                Arg.Any<Guid>(),
                Arg.Any<DataScope>(),
                Arg.Any<IReadOnlyDictionary<string, object?>?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedSchemaId = callInfo.ArgAt<Guid>(0);
                capturedScope = callInfo.ArgAt<DataScope>(1);
                return Array.Empty<EntityRecord>();
            });

        var node = new QueryRecordsNode(Guid.NewGuid(), "Заявка", "records");
        var context = CreateContext(dataStore, schemaStore, projectId, scenarioVersion: 4);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(capturedSchemaId).IsEqualTo(stableSchemaId);
        await Assert.That(capturedScope).IsNotNull();
        await Assert.That(capturedScope!.BotId).IsEqualTo(context.Session.BotId);
        await Assert.That(capturedScope.PlatformUserId).IsEqualTo(context.Session.PlatformUserId);
    }

    private static ExecutionContext CreateContext(
        IDataStore dataStore,
        ISchemaStore schemaStore,
        Guid? projectId = null,
        int scenarioVersion = 0) =>
        new()
        {
            Session = new TestSession(),
            DataStore = dataStore,
            SchemaStore = schemaStore,
            BotToken = string.Empty,
            ProjectId = projectId ?? Guid.NewGuid(),
            ScenarioVersion = scenarioVersion,
        };
}
