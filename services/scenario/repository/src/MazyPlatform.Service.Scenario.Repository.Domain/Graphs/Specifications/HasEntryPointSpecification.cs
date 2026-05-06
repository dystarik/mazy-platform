namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

using System.Text.Json;

public sealed class HasEntryPointSpecification : IScenarioSpecification
{
    public Result IsSatisfiedBy(string graphJson)
    {
        if (string.IsNullOrWhiteSpace(graphJson))
            return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, "Граф сценария должен содержать хотя бы одну точку входа.");

        try
        {
            using var document = JsonDocument.Parse(graphJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array)
                return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, "Граф сценария должен содержать хотя бы одну точку входа.");

            foreach (var node in nodes.EnumerateArray())
            {
                if (node.TryGetProperty("type", out var typeProperty) &&
                    string.Equals(typeProperty.GetString(), "entry_point", StringComparison.Ordinal))
                {
                    return Result.Success();
                }
            }

            return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, "Граф сценария должен содержать хотя бы одну точку входа.");
        }
        catch (JsonException)
        {
            return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, "Граф сценария должен содержать хотя бы одну точку входа.");
        }
    }
}
