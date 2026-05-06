namespace MazyPlatform.Scenario.Tests.Nodes.Data;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Data;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class GetRecordNodeTests
{
    [Test]
    public async Task ExecuteAsync_GetsRecord_AndSavesData()
    {
        var recordId = Guid.NewGuid();
        var data = new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" };
        var record = new EntityRecord(recordId, Guid.NewGuid(), data);

        var dataStore = Substitute.For<IDataStore>();
        dataStore.GetAsync(recordId, Arg.Any<DataScope>(), Arg.Any<CancellationToken>()).Returns(record);

        var node = new GetRecordNode(Guid.NewGuid(), "recordId", "result");
        var context = CreateContext(
            dataStore,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["recordId"] = recordId.ToString(),
            });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["result"]).IsEqualTo(data);
    }

    [Test]
    public async Task ExecuteAsync_InvalidRecordId_ReturnsError()
    {
        var node = new GetRecordNode(Guid.NewGuid(), "recordId", "result");
        var context = CreateContext(
            Substitute.For<IDataStore>(),
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["recordId"] = "not-a-guid",
            });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Error);
    }

    [Test]
    public async Task ExecuteAsync_RecordNotFound_ReturnsError()
    {
        var recordId = Guid.NewGuid();
        var dataStore = Substitute.For<IDataStore>();
        dataStore.GetAsync(recordId, Arg.Any<DataScope>(), Arg.Any<CancellationToken>()).Returns((EntityRecord?)null);

        var node = new GetRecordNode(Guid.NewGuid(), "recordId", "result");
        var context = CreateContext(
            dataStore,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["recordId"] = recordId.ToString(),
            });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Error);
    }

    private static ExecutionContext CreateContext(
        IDataStore dataStore,
        IDictionary<string, object?>? variables = null)
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
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
