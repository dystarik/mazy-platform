namespace MazyPlatform.Scenario.Nodes.Data;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла создания записи.
/// </summary>
public sealed class CreateRecordNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "create_record";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "entityName",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя пользовательской сущности."),
        new(
            "fields",
            NodeParamType.StringDictionary,
            IsRequired: true,
            Description: "Поля записи: имя поля → значение."),
        new(
            "recordIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID созданной записи."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new CreateRecordNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "entityName"),
            JsonParamHelper.GetRequiredStringDictionary(parameters, "fields"),
            JsonParamHelper.GetOptionalString(parameters, "recordIdVariable"));

    /// <inheritdoc />
    public override IReadOnlyList<string> Validate(Guid nodeId, JsonElement parameters)
    {
        var errors = base.Validate(nodeId, parameters).ToList();

        if (!parameters.TryGetProperty("entityName", out var nameElement)
            || string.IsNullOrWhiteSpace(nameElement.GetString()))
        {
            if (!errors.Any())
            {
                errors.Add($"Узел {nodeId}: параметр \"entityName\" обязателен.");
            }
        }

        if (!parameters.TryGetProperty("fields", out var fieldsElement)
            || fieldsElement.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"Узел {nodeId}: параметр \"fields\" обязателен и должен быть объектом.");
        }

        return errors;
    }
}
