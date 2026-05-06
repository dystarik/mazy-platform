namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

using System.Text.Json;

public sealed class AllConnectionsValidSpecification : IScenarioSpecification
{
    public Result IsSatisfiedBy(string graphJson)
    {
        if (string.IsNullOrWhiteSpace(graphJson))
            return Result.Success();

        try
        {
            using var document = JsonDocument.Parse(graphJson);
            var root = document.RootElement;

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);

            if (root.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in nodes.EnumerateArray())
                {
                    if (node.TryGetProperty("id", out var idProperty))
                    {
                        var id = idProperty.GetString();
                        if (!string.IsNullOrEmpty(id))
                            nodeIds.Add(id);
                    }
                }
            }

            if (!root.TryGetProperty("connections", out var connections) || connections.ValueKind != JsonValueKind.Array)
                return Result.Success();

            foreach (var connection in connections.EnumerateArray())
            {
                if (connection.TryGetProperty("from", out var fromProperty))
                {
                    var from = fromProperty.GetString();
                    if (!string.IsNullOrEmpty(from) && !nodeIds.Contains(from))
                        return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, $"Соединение содержит несуществующий узел '{from}'.");
                }

                if (connection.TryGetProperty("to", out var toProperty))
                {
                    var to = toProperty.GetString();
                    if (!string.IsNullOrEmpty(to) && !nodeIds.Contains(to))
                        return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, $"Соединение содержит несуществующий узел '{to}'.");
                }
            }

            return Result.Success();
        }
        catch (JsonException)
        {
            return Result.Success();
        }
    }
}
