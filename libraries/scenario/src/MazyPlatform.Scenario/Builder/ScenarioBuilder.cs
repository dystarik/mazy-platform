namespace MazyPlatform.Scenario.Builder;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Nodes.Messages;

/// <summary>
/// Собирает граф сценария из JSON.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр сборщика сценариев.
/// </remarks>
/// <param name="nodeRegistry">Реестр типов узлов.</param>
public sealed class ScenarioBuilder(INodeRegistry nodeRegistry) : IScenarioBuilder
{
    /// <inheritdoc />
    public ScenarioGraph Build(string scenarioJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioJson);

        using var document = JsonDocument.Parse(scenarioJson);
        var root = document.RootElement;

        var startNodeId = root.GetProperty("startNodeId").GetGuid();
        var nodes = BuildNodes(root.GetProperty("nodes"));
        var connections = BuildConnections(root.GetProperty("connections"));
        var entryPoints = BuildEntryPoints(nodes);

        return new ScenarioGraph
        {
            StartNodeId = startNodeId,
            Nodes = nodes,
            Connections = connections,
            EntryPoints = entryPoints,
        };
    }

    private static Dictionary<Guid, IReadOnlyList<NodeConnection>> BuildConnections(JsonElement connectionsArray)
    {
        var grouped = new Dictionary<Guid, List<NodeConnection>>();

        foreach (var connElement in connectionsArray.EnumerateArray())
        {
            var from = connElement.GetProperty("from").GetGuid();
            var to = connElement.GetProperty("to").GetGuid();

            var branch = connElement.TryGetProperty("branch", out var branchElement)
                ? branchElement.GetString() ?? "default"
                : "default";

            if (!grouped.TryGetValue(from, out var list))
            {
                list = [];
                grouped[from] = list;
            }

            list.Add(new NodeConnection(from, to, branch));
        }

        return grouped.ToDictionary(
            kvp => kvp.Key, IReadOnlyList<NodeConnection> (kvp) => kvp.Value);
    }

    /// <summary>
    /// Собирает словарь точек входа сценария: payload → nodeId.
    /// </summary>
    /// <remarks>
    /// В точки входа попадают только узлы <see cref="ReceiveButtonPressNode"/>
    /// с <see cref="ReceiveButtonPressNode.IsEntry"/> = <c>true</c> и непустым
    /// списком <see cref="ReceiveButtonPressNode.ExpectedPayloads"/>. Каждый
    /// payload из этого списка регистрируется отдельной записью в словаре —
    /// один entry-узел может быть привязан к нескольким payload'ам.
    /// <para>
    /// Бросает <see cref="InvalidOperationException"/>, если один и тот же
    /// payload объявлен в двух разных entry-узлах: маршрутизация по payload
    /// должна быть однозначной.
    /// </para>
    /// <para>
    /// Логика обработки entry-маршрутов на уровне исполнителя — см.
    /// <c>ScenarioExecutor.NavigateToEntryOrStart</c>.
    /// </para>
    /// </remarks>
    private static Dictionary<string, Guid> BuildEntryPoints(Dictionary<Guid, INode> nodes)
    {
        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var (nodeId, node) in nodes)
        {
            if (node is not ReceiveButtonPressNode { IsEntry: true, ExpectedPayloads: { Count: > 0 } entryPayloads })
                continue;

            foreach (var payload in entryPayloads)
            {
                if (!entryPoints.TryAdd(payload, nodeId))
                {
                    throw new InvalidOperationException(
                        $"Дублирующийся payload \"{payload}\" в entry points: узлы {entryPoints[payload]} и {nodeId}.");
                }
            }
        }

        return entryPoints;
    }

    private Dictionary<Guid, INode> BuildNodes(JsonElement nodesArray)
    {
        var nodes = new Dictionary<Guid, INode>();

        foreach (var nodeElement in nodesArray.EnumerateArray())
        {
            var id = nodeElement.GetProperty("id").GetGuid();
            var type = nodeElement.GetProperty("type").GetString()
                ?? throw new InvalidOperationException($"Узел {id}: отсутствует поле \"type\".");

            var parameters = nodeElement.TryGetProperty("params", out var paramsElement)
                ? paramsElement
                : default;

            nodes[id] = nodeRegistry.Resolve(type, id, parameters);
        }

        return nodes;
    }
}
