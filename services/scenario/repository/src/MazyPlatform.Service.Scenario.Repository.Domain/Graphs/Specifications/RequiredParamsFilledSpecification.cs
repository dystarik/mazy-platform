namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

using System.Text.Json;

public sealed class RequiredParamsFilledSpecification : IScenarioSpecification
{
    public Result IsSatisfiedBy(string graphJson)
    {
        if (string.IsNullOrWhiteSpace(graphJson))
            return Result.Success();

        try
        {
            using var document = JsonDocument.Parse(graphJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array)
                return Result.Success();

            foreach (var node in nodes.EnumerateArray())
            {
                var nodeId = node.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;

                if (!node.TryGetProperty("requiredParams", out var requiredParams) ||
                    requiredParams.ValueKind != JsonValueKind.Array)
                    continue;

                node.TryGetProperty("params", out var paramsElement);

                foreach (var requiredParam in requiredParams.EnumerateArray())
                {
                    var paramName = requiredParam.GetString();
                    if (string.IsNullOrEmpty(paramName))
                        continue;

                    var isFilled = paramsElement.ValueKind == JsonValueKind.Object &&
                                   paramsElement.TryGetProperty(paramName, out var paramValue) &&
                                   paramValue.ValueKind != JsonValueKind.Null &&
                                   !string.IsNullOrEmpty(paramValue.GetString());

                    if (!isFilled)
                        return Error.Validation(ErrorCodes.ScenarioGraph.PromotionFailed, $"У узла '{nodeId}' не заполнен обязательный параметр '{paramName}'.");
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
