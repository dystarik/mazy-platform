namespace MazyPlatform.Scenario.Validation;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;

internal static class NodeParamLimitValidator
{
    public static void ValidateLimits(
        Guid nodeId,
        JsonElement parameters,
        IReadOnlyList<NodeParamSchema> schema,
        List<string> errors)
    {
        foreach (var param in schema)
        {
            ValidateParam(nodeId, parameters, param, param.Key, errors);
        }
    }

    public static void ValidateObjectMatrixLimits(
        Guid nodeId,
        JsonElement value,
        NodeParamSchema param,
        string path,
        List<string> errors)
    {
        if (param.MaxRows is { } maxRows && value.GetArrayLength() > maxRows)
        {
            errors.Add(
                $"Узел {nodeId}: параметр \"{path}\" содержит слишком много рядов: " +
                $"{value.GetArrayLength()}. Допустимо не больше {maxRows}.");
        }

        var totalItems = 0;
        var rowIndex = 0;
        foreach (var row in value.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
            {
                rowIndex++;
                continue;
            }

            var rowItems = row.GetArrayLength();
            totalItems += rowItems;

            if (param.MaxItemsPerRow is { } maxItemsPerRow && rowItems > maxItemsPerRow)
            {
                errors.Add(
                    $"Узел {nodeId}: параметр \"{path}[{rowIndex}]\" содержит слишком много элементов: " +
                    $"{rowItems}. Допустимо не больше {maxItemsPerRow}.");
            }

            rowIndex++;
        }

        if (param.MaxItemsTotal is { } maxItemsTotal && totalItems > maxItemsTotal)
        {
            errors.Add(
                $"Узел {nodeId}: параметр \"{path}\" содержит слишком много элементов: " +
                $"{totalItems}. Допустимо не больше {maxItemsTotal}.");
        }
    }

    private static void ValidateParam(
        Guid nodeId,
        JsonElement parent,
        NodeParamSchema param,
        string path,
        List<string> errors)
    {
        if (!parent.TryGetProperty(param.Key, out var value))
        {
            return;
        }

        switch (param.Type)
        {
            case NodeParamType.ObjectMatrix when value.ValueKind == JsonValueKind.Array:
                ValidateObjectMatrixLimits(nodeId, value, param, path, errors);
                break;

            case NodeParamType.Object when value.ValueKind == JsonValueKind.Object && param.Fields is not null:
                ValidateLimits(nodeId, value, param.Fields, errors);
                break;

            case NodeParamType.ObjectList when value.ValueKind == JsonValueKind.Array && param.Fields is not null:
                var index = 0;
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in param.Fields)
                        {
                            ValidateParam(nodeId, item, field, $"{path}[{index}].{field.Key}", errors);
                        }
                    }

                    index++;
                }

                break;
        }
    }
}
