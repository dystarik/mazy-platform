namespace MazyPlatform.Scenario.Tests.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Data;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class CreateRecordNodeTests
{
    [Test]
    public async Task ExecuteAsync_CreatesRecord_AndSavesRecordId()
    {
        var schemaId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var schema = new EntitySchema(schemaId, Guid.NewGuid(), "Заявка", []);

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();

        schemaStore
            .GetAsync(Arg.Any<Guid>(), Arg.Any<int>(), "Заявка", Arg.Any<CancellationToken>())
            .Returns(schema);

        dataStore
            .CreateAsync(
                schemaId,
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<DataScope>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new EntityRecord(
                recordId,
                schemaId,
                new Dictionary<string, object?>(StringComparer.Ordinal)));

        var fieldMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = "Тест",
        };
        var node = new CreateRecordNode(Guid.NewGuid(), "Заявка", fieldMappings, "recordId");
        var context = CreateContext(dataStore, schemaStore);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["recordId"])
            .IsEqualTo(recordId.ToString());
    }

    [Test]
    public async Task ExecuteAsync_SchemaNotFound_ReturnsError()
    {
        var schemaStore = Substitute.For<ISchemaStore>();
        schemaStore
            .GetAsync(Arg.Any<Guid>(), Arg.Any<int>(), "НеСуществует", Arg.Any<CancellationToken>())
            .Returns((EntitySchema?)null);

        var fieldMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var node = new CreateRecordNode(Guid.NewGuid(), "НеСуществует", fieldMappings);
        var context = CreateContext(Substitute.For<IDataStore>(), schemaStore);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Error);
    }

    [Test]
    public async Task ExecuteAsync_ReplacesVariablesInFields()
    {
        var schemaId = Guid.NewGuid();
        var schema = new EntitySchema(schemaId, Guid.NewGuid(), "Заявка", []);

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();

        schemaStore
            .GetAsync(Arg.Any<Guid>(), Arg.Any<int>(), "Заявка", Arg.Any<CancellationToken>())
            .Returns(schema);

        IReadOnlyDictionary<string, object?>? capturedData = null;
        dataStore
            .CreateAsync(
                schemaId,
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<DataScope>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedData = callInfo.ArgAt<IReadOnlyDictionary<string, object?>>(1);
                return new EntityRecord(
                    Guid.NewGuid(),
                    schemaId,
                    new Dictionary<string, object?>(StringComparer.Ordinal));
            });

        var fieldMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["client"] = "{name}",
        };
        var node = new CreateRecordNode(Guid.NewGuid(), "Заявка", fieldMappings);
        var context = CreateContext(
            dataStore,
            schemaStore,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" });

        await node.ExecuteAsync(context);

        await Assert.That(capturedData).IsNotNull();
        await Assert.That(capturedData!["client"]!.ToString()).IsEqualTo("Иван");
    }

    [Test]
    public async Task ExecuteAsync_UsesVersionedSchemaSnapshotAndUserScope()
    {
        var projectId = Guid.NewGuid();
        var schemaSnapshotId = Guid.NewGuid();
        var stableSchemaId = Guid.NewGuid();
        var schema = new EntitySchema(schemaSnapshotId, stableSchemaId, projectId, 5, "Заявка", []);

        var dataStore = Substitute.For<IDataStore>();
        var schemaStore = Substitute.For<ISchemaStore>();

        schemaStore
            .GetAsync(projectId, 5, "Заявка", Arg.Any<CancellationToken>())
            .Returns(schema);

        Guid? capturedSchemaSnapshotId = null;
        DataScope? capturedScope = null;
        dataStore
            .CreateAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<DataScope>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedSchemaSnapshotId = callInfo.ArgAt<Guid>(0);
                capturedScope = callInfo.ArgAt<DataScope>(2);
                return new EntityRecord(
                    Guid.NewGuid(),
                    stableSchemaId,
                    new Dictionary<string, object?>(StringComparer.Ordinal))
                {
                    SchemaSnapshotId = schemaSnapshotId,
                };
            });

        var node = new CreateRecordNode(
            Guid.NewGuid(),
            "Заявка",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["name"] = "Тест" });
        var context = CreateContext(dataStore, schemaStore, projectId: projectId, scenarioVersion: 5);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(capturedSchemaSnapshotId).IsEqualTo(schemaSnapshotId);
        await Assert.That(capturedScope).IsNotNull();
        await Assert.That(capturedScope!.ProjectId).IsEqualTo(projectId);
        await Assert.That(capturedScope.ScenarioVersion).IsEqualTo(5);
        await Assert.That(capturedScope.BotId).IsEqualTo(context.Session.BotId);
        await Assert.That(capturedScope.PlatformUserId).IsEqualTo(context.Session.PlatformUserId);
    }

    private static ExecutionContext CreateContext(
        IDataStore dataStore,
        ISchemaStore schemaStore,
        IDictionary<string, object?>? variables = null,
        Guid? projectId = null,
        int scenarioVersion = 0)
    {
        var session = new TestSession();

        if (variables is not null)
        {
            foreach (var (key, value) in variables)
            {
                session.Variables[key] = value;
            }
        }

        return new ExecutionContext
        {
            Session = session,
            DataStore = dataStore,
            SchemaStore = schemaStore,
            BotToken = string.Empty,
            ProjectId = projectId ?? Guid.NewGuid(),
            ScenarioVersion = scenarioVersion,
        };
    }
}
