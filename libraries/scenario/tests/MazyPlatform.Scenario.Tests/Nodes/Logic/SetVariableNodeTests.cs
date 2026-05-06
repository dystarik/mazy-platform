namespace MazyPlatform.Scenario.Tests.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class SetVariableNodeTests
{
    [Test]
    public async Task ExecuteAsync_SetsVariableValue()
    {
        var node = new SetVariableNode(Guid.NewGuid(), "status", "new");
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["status"]).IsEqualTo("new");
    }

    [Test]
    public async Task ExecuteAsync_ReplacesVariablesInTemplate()
    {
        var node = new SetVariableNode(Guid.NewGuid(), "greeting", "Привет, {name}!");
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" });

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables["greeting"]).IsEqualTo("Привет, Иван!");
    }

    private static ExecutionContext CreateContext(IDictionary<string, object?>? variables = null)
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
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
