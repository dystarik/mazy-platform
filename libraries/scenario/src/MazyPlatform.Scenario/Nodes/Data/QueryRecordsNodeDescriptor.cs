namespace MazyPlatform.Scenario.Nodes.Data;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла поиска записей.
/// </summary>
public sealed class QueryRecordsNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "query_records";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "entityName",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя пользовательской сущности."),
        new(
            "recordsVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для сохранения списка найденных записей."),
        new(
            "filter",
            NodeParamType.StringDictionary,
            IsRequired: false,
            Description: "Фильтр поиска: имя поля → значение."),
        new(
            "countVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения количества найденных записей (как строка)."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new QueryRecordsNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "entityName"),
            JsonParamHelper.GetRequiredString(parameters, "recordsVariable"),
            JsonParamHelper.GetOptionalStringDictionary(parameters, "filter"),
            JsonParamHelper.GetOptionalString(parameters, "countVariable"));
}
