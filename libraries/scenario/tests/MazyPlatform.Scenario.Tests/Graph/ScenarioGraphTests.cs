namespace MazyPlatform.Scenario.Tests.Graph;

using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Tests.Helpers;

public class ScenarioGraphTests
{
    [Test]
    public async Task GetNextNodeId_FindsConnectionByBranch()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var graph = CreateGraph(fromId, toId, "default");

        var nextId = graph.GetNextNodeId(fromId, "default");

        await Assert.That(nextId).IsEqualTo(toId);
    }

    [Test]
    public async Task GetNextNodeId_NoConnections_ReturnsNull()
    {
        var nodeId = Guid.NewGuid();
        var graph = new ScenarioGraph
        {
            StartNodeId = nodeId,
            Nodes = new Dictionary<Guid, INode> { [nodeId] = CreateDummyNode(nodeId) },
            Connections = new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            EntryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal),
        };

        var nextId = graph.GetNextNodeId(nodeId, "default");

        await Assert.That(nextId).IsNull();
    }

    [Test]
    public async Task GetNextNodeId_NullBranch_UsesDefault()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var graph = CreateGraph(fromId, toId, "default");

        var nextId = graph.GetNextNodeId(fromId, null);

        await Assert.That(nextId).IsEqualTo(toId);
    }

    [Test]
    public async Task GetNextNodeId_CaseInsensitiveBranchComparison()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var graph = CreateGraph(fromId, toId, "True");

        var nextId = graph.GetNextNodeId(fromId, "true");

        await Assert.That(nextId).IsEqualTo(toId);
    }

    private static ScenarioGraph CreateGraph(Guid fromId, Guid toId, string branchKey)
    {
        return new ScenarioGraph
        {
            StartNodeId = fromId,
            Nodes = new Dictionary<Guid, INode>
            {
                [fromId] = CreateDummyNode(fromId),
                [toId] = CreateDummyNode(toId),
            },
            Connections = new Dictionary<Guid, IReadOnlyList<NodeConnection>>
            {
                [fromId] = [new NodeConnection(fromId, toId, branchKey)],
            },
            EntryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal),
        };
    }

    private static INode CreateDummyNode(Guid id) =>
        new TestNode(id, "test", false, _ => Task.FromResult(NodeResult.Continue()));
}
