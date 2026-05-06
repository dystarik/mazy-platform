namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

using System.Text.Json;

public sealed class NoCyclesWithoutAwaitSpecification : IScenarioSpecification
{
    private enum Color
    {
        White,
        Gray,
        Black,
    }

    public Result IsSatisfiedBy(string graphJson)
    {
        if (string.IsNullOrWhiteSpace(graphJson))
            return Result.Success();

        try
        {
            using var document = JsonDocument.Parse(graphJson);
            var root = document.RootElement;

            var nodeTypes = new Dictionary<string, string>(StringComparer.Ordinal);
            var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            if (root.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in nodes.EnumerateArray())
                {
                    var id = node.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    var type = node.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;
                    nodeTypes[id] = type;
                    adjacency[id] = [];
                }
            }

            if (root.TryGetProperty("connections", out var connections) && connections.ValueKind == JsonValueKind.Array)
            {
                foreach (var connection in connections.EnumerateArray())
                {
                    var from = connection.TryGetProperty("from", out var fromProp) ? fromProp.GetString() ?? string.Empty : string.Empty;
                    var to = connection.TryGetProperty("to", out var toProp) ? toProp.GetString() ?? string.Empty : string.Empty;

                    if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) && adjacency.ContainsKey(from))
                        adjacency[from].Add(to);
                }
            }

            var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
            var path = new List<string>();

            foreach (var nodeId in nodeTypes.Keys)
                colors[nodeId] = Color.White;

            foreach (var nodeId in nodeTypes.Keys)
            {
                if (colors[nodeId] == Color.White)
                {
                    var cycleResult = Dfs(nodeId, colors, path, adjacency, nodeTypes);
                    if (cycleResult.IsFailure)
                        return cycleResult;
                }
            }

            return Result.Success();
        }
        catch (JsonException)
        {
            return Result.Success();
        }
    }

    private static Result Dfs(
        string nodeId,
        Dictionary<string, Color> colors,
        List<string> path,
        Dictionary<string, List<string>> adjacency,
        Dictionary<string, string> nodeTypes)
    {
        colors[nodeId] = Color.Gray;
        path.Add(nodeId);

        if (adjacency.TryGetValue(nodeId, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!colors.TryGetValue(neighbor, out var neighborColor))
                    continue;

                if (neighborColor == Color.Gray)
                {
                    // Back edge found — extract the cycle and check for await nodes
                    var cycleStart = path.IndexOf(neighbor);
                    var cycle = path.Skip(cycleStart).ToList();

                    var hasAwait = cycle.Any(id => nodeTypes.TryGetValue(id, out var t) && string.Equals(t, "await", StringComparison.Ordinal));
                    if (!hasAwait)
                        return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, "Граф сценария содержит бесконечный цикл без узла ожидания.");
                }
                else if (neighborColor == Color.White)
                {
                    var result = Dfs(neighbor, colors, path, adjacency, nodeTypes);
                    if (result.IsFailure)
                        return result;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        colors[nodeId] = Color.Black;

        return Result.Success();
    }
}
