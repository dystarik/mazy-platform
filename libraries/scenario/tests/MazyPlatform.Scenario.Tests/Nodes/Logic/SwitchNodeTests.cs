namespace MazyPlatform.Scenario.Tests.Nodes.Logic;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class SwitchNodeTests
{
    [Test]
    public async Task ExecuteAsync_Match_ReturnsCorrespondingBranch()
    {
        var cases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["new"] = "case_new",
            ["done"] = "case_done",
        };
        var node = new SwitchNode(Guid.NewGuid(), "status", cases);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["status"] = "new" });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.BranchKey).IsEqualTo("case_new");
    }

    [Test]
    public async Task ExecuteAsync_NoMatch_ReturnsDefault()
    {
        var cases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["new"] = "case_new",
        };
        var node = new SwitchNode(Guid.NewGuid(), "status", cases);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["status"] = "unknown" });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.BranchKey).IsEqualTo("default");
    }

    [Test]
    public async Task ExecuteAsync_VariableMissing_ReturnsDefault()
    {
        var cases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["new"] = "case_new",
        };
        var node = new SwitchNode(Guid.NewGuid(), "status", cases);
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.BranchKey).IsEqualTo("default");
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
