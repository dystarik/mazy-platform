namespace MazyPlatform.Scenario.Nodes.Data;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла обновления записи.
/// </summary>
public sealed class UpdateRecordNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "update_record";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "recordIdVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной с ID записи для обновления."),
        new(
            "fields",
            NodeParamType.StringDictionary,
            IsRequired: true,
            Description: "Поля для обновления: имя поля → новое значение."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new UpdateRecordNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "recordIdVariable"),
            JsonParamHelper.GetRequiredStringDictionary(parameters, "fields"));
}
