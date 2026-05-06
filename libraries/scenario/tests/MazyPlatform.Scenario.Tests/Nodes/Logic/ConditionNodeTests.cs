namespace MazyPlatform.Scenario.Tests.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ConditionNodeTests
{
    [Test]
    public async Task ExecuteAsync_TrueCondition_ReturnsTrueBranch()
    {
        var node = new ConditionNode(Guid.NewGuid(), "Иван", "==", "Иван");
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(result.BranchKey).IsEqualTo("true");
    }

    [Test]
    public async Task ExecuteAsync_FalseCondition_ReturnsFalseBranch()
    {
        var node = new ConditionNode(Guid.NewGuid(), "Иван", "==", "Пётр");
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(result.BranchKey).IsEqualTo("false");
    }

    private static ExecutionContext CreateContext()
    {
        return new ExecutionContext
        {
            Session = new TestSession(),
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
