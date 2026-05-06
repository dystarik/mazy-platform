namespace MazyPlatform.Scenario.Tests.Helpers;

using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Nodes;

public static class ScenarioGraphFactory
{
    public static ScenarioGraph CreateLinear(params INode[] nodes) =>
        CreateLinear(new Dictionary<string, Guid>(StringComparer.Ordinal), nodes);

    public static ScenarioGraph CreateLinear(IReadOnlyDictionary<string, Guid> entryPoints, params INode[] nodes)
    {
        var nodeDict = new Dictionary<Guid, INode>();
        var connections = new Dictionary<Guid, IReadOnlyList<NodeConnection>>();

        for (var i = 0; i < nodes.Length; i++)
        {
            nodeDict[nodes[i].NodeId] = nodes[i];

            if (i < nodes.Length - 1)
            {
                connections[nodes[i].NodeId] =
                [
                    new NodeConnection(nodes[i].NodeId, nodes[i + 1].NodeId, "default"),
                ];
            }
        }

        return new ScenarioGraph
        {
            StartNodeId = nodes[0].NodeId,
            Nodes = nodeDict,
            Connections = connections,
            EntryPoints = entryPoints,
        };
    }

    public static ScenarioGraph Create(
        Guid startNodeId,
        IReadOnlyDictionary<Guid, INode> nodes,
        IReadOnlyDictionary<Guid, IReadOnlyList<NodeConnection>> connections,
        IReadOnlyDictionary<string, Guid>? entryPoints = null)
    {
        return new ScenarioGraph
        {
            StartNodeId = startNodeId,
            Nodes = nodes,
            Connections = connections,
            EntryPoints = entryPoints ?? new Dictionary<string, Guid>(StringComparer.Ordinal),
        };
    }
}
