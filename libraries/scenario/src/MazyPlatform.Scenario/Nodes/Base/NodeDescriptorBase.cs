namespace MazyPlatform.Scenario.Nodes.Base;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Базовый класс дескриптора узла.
/// Содержит типовые проверки обязательных параметров по схеме.
/// </summary>
public abstract class NodeDescriptorBase : INodeDescriptor
{
    private readonly Lock _schemaValidationLock = new();
    private bool _isSchemaValidated;

    /// <inheritdoc />
    public abstract string Type { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<NodeParamSchema> Schema { get; }

    /// <summary>
    /// Проверяет корректность схемы узла.
    /// </summary>
    /// <param name="nodeType">Тип узла.</param>
    /// <param name="schema">Схема параметров.</param>
    public static void EnsureSchemaValid(string nodeType, IReadOnlyList<NodeParamSchema> schema) =>
        EnsureSchemaValid(nodeType, schema, parentPath: null);

    /// <inheritdoc />
    public abstract INode Create(Guid id, JsonElement parameters, IServiceProvider services);

    /// <inheritdoc />
    public virtual IReadOnlyList<string> Validate(Guid nodeId, JsonElement parameters)
    {
        EnsureSchemaValidated();

        var errors = new List<string>();

        foreach (var param in Schema)
        {
            ValidateParam(nodeId, parameters, param, param.Key, errors);
        }

        return errors;
    }

    internal void EnsureSchemaIsValid() => EnsureSchemaValidated();

    private static void ValidateParam(
        Guid nodeId,
        JsonElement parent,
        NodeParamSchema param,
        string path,
        List<string> errors)
    {
        if (!parent.TryGetProperty(param.Key, out var property))
        {
            if (param.IsRequired)
            {
                errors.Add($"Узел {nodeId}: обязательный параметр \"{path}\" отсутствует.");
            }

            return;
        }

        ValidateValue(nodeId, property, param, path, errors);
    }

    private static void ValidateValue(
        Guid nodeId,
        JsonElement value,
        NodeParamSchema param,
        string path,
        List<string> errors)
    {
        if (!IsValidBasicType(value, param))
        {
            errors.Add($"Узел {nodeId}: параметр \"{path}\" имеет неверный тип. Ожидается {GetTypeName(param)}.");
            return;
        }

        if (param.Type == NodeParamType.Enum)
        {
            var enumValues = param.EnumValues!;
            var enumValue = value.GetString()!;

            if (!enumValues.Contains(enumValue, StringComparer.Ordinal))
            {
                errors.Add(
                    $"Узел {nodeId}: параметр \"{path}\" должен быть одним из значений: " +
                    string.Join(", ", enumValues) + ".");
            }

            return;
        }

        if (param.Type == NodeParamType.Object)
        {
            ValidateObject(nodeId, value, param.Fields!, path, errors);
            return;
        }

        if (param.Type == NodeParamType.ObjectList)
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                var itemPath = $"{path}[{index}]";

                ValidateValue(
                    nodeId,
                    item,
                    new NodeParamSchema(itemPath, NodeParamType.Object, IsRequired: true, Fields: param.Fields),
                    itemPath,
                    errors);

                index++;
            }

            return;
        }

        if (param.Type == NodeParamType.ObjectMatrix)
        {
            ValidateObjectMatrix(nodeId, value, param.Fields!, path, param.AllowEmptyCollection, errors);
        }
    }

    private static void ValidateObjectMatrix(
        Guid nodeId,
        JsonElement value,
        IReadOnlyList<NodeParamSchema> fields,
        string path,
        bool allowEmptyCollection,
        List<string> errors)
    {
        if (value.GetArrayLength() == 0 && !allowEmptyCollection)
        {
            errors.Add($"Узел {nodeId}: параметр \"{path}\" не должен быть пустым.");
            return;
        }

        var rowIndex = 0;
        foreach (var row in value.EnumerateArray())
        {
            var rowPath = $"{path}[{rowIndex}]";

            if (row.ValueKind != JsonValueKind.Array)
            {
                errors.Add(
                    $"Узел {nodeId}: параметр \"{rowPath}\" имеет неверный тип. Ожидается массив объектов.");
                rowIndex++;
                continue;
            }

            if (row.GetArrayLength() == 0)
            {
                errors.Add($"Узел {nodeId}: параметр \"{rowPath}\" не должен быть пустым.");
                rowIndex++;
                continue;
            }

            var itemIndex = 0;
            foreach (var item in row.EnumerateArray())
            {
                var itemPath = $"{rowPath}[{itemIndex}]";

                ValidateValue(
                    nodeId,
                    item,
                    new NodeParamSchema(itemPath, NodeParamType.Object, IsRequired: true, Fields: fields),
                    itemPath,
                    errors);

                itemIndex++;
            }

            rowIndex++;
        }
    }

    private static void ValidateObject(
        Guid nodeId,
        JsonElement element,
        IReadOnlyList<NodeParamSchema> fields,
        string parentPath,
        List<string> errors)
    {
        foreach (var field in fields)
        {
            if (!element.TryGetProperty(field.Key, out var fieldValue))
            {
                if (field.IsRequired)
                {
                    errors.Add($"Узел {nodeId}: в параметре \"{parentPath}\" отсутствует обязательное поле \"{field.Key}\".");
                }

                continue;
            }

            var fieldPath = $"{parentPath}.{field.Key}";

            ValidateValue(nodeId, fieldValue, field, fieldPath, errors);
        }
    }

    private static bool IsValidBasicType(JsonElement property, NodeParamSchema param)
    {
        return param.Type switch
        {
            NodeParamType.String => property.ValueKind == JsonValueKind.String,
            NodeParamType.Int => property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out _),
            NodeParamType.Bool => property.ValueKind is JsonValueKind.True or JsonValueKind.False,
            NodeParamType.StringDictionary =>
                property.ValueKind == JsonValueKind.Object
                && property.EnumerateObject().All(static p => p.Value.ValueKind == JsonValueKind.String),
            NodeParamType.StringList =>
                property.ValueKind == JsonValueKind.Array
                && property.EnumerateArray().All(static e => e.ValueKind == JsonValueKind.String),
            NodeParamType.Object => property.ValueKind == JsonValueKind.Object,
            NodeParamType.Enum => property.ValueKind == JsonValueKind.String,
            NodeParamType.ObjectList => property.ValueKind == JsonValueKind.Array,
            NodeParamType.ObjectMatrix => property.ValueKind == JsonValueKind.Array,
            _ => false,
        };
    }

    private static string GetTypeName(NodeParamSchema param) =>
        param.Type switch
        {
            NodeParamType.String => "строка",
            NodeParamType.Int => "целое число",
            NodeParamType.Bool => "логическое значение",
            NodeParamType.StringDictionary => "объект со строковыми значениями",
            NodeParamType.StringList => "массив строк",
            NodeParamType.Object => "объект",
            NodeParamType.ObjectList => "массив объектов",
            NodeParamType.ObjectMatrix => "двумерный массив объектов",
            NodeParamType.Enum => "строка из набора",
            _ => "корректный тип",
        };

    private static void EnsureSchemaValid(string nodeType, IReadOnlyList<NodeParamSchema> schema, string? parentPath)
    {
        foreach (var param in schema)
        {
            var path = parentPath is null ? param.Key : $"{parentPath}.{param.Key}";

            switch (param.Type)
            {
                case NodeParamType.Object:
                case NodeParamType.ObjectList:
                case NodeParamType.ObjectMatrix:
                    if (param.Fields is not { Count: > 0 })
                    {
                        throw new InvalidOperationException(
                            $"Узел \"{nodeType}\", параметр \"{path}\": тип {param.Type} требует Fields, но они не заданы.");
                    }

                    if (param.EnumValues is not null)
                    {
                        throw new InvalidOperationException(
                            $"Узел \"{nodeType}\", параметр \"{path}\": для типа {param.Type} поле EnumValues должно быть null.");
                    }

                    EnsureSchemaValid(nodeType, param.Fields, path);
                    break;

                case NodeParamType.Enum:
                    if (param.EnumValues is not { Count: > 0 })
                    {
                        throw new InvalidOperationException(
                            $"Узел \"{nodeType}\", параметр \"{path}\": тип Enum требует EnumValues, но они не заданы.");
                    }

                    if (param.Fields is not null)
                    {
                        throw new InvalidOperationException(
                            $"Узел \"{nodeType}\", параметр \"{path}\": для типа Enum поле Fields должно быть null.");
                    }

                    break;

                default:
                    if (param.Fields is not null)
                    {
                        throw new InvalidOperationException(
                            $"Узел \"{nodeType}\", параметр \"{path}\": для типа {param.Type} поле Fields должно быть null.");
                    }

                    if (param.EnumValues is not null)
                    {
                        throw new InvalidOperationException(
                            $"Узел \"{nodeType}\", параметр \"{path}\": для типа {param.Type} поле EnumValues должно быть null.");
                    }

                    break;
            }
        }
    }

    private void EnsureSchemaValidated()
    {
        if (_isSchemaValidated)
        {
            return;
        }

        lock (_schemaValidationLock)
        {
            if (_isSchemaValidated)
            {
                return;
            }

            EnsureSchemaValid(Type, Schema, parentPath: null);
            _isSchemaValidated = true;
        }
    }
}
