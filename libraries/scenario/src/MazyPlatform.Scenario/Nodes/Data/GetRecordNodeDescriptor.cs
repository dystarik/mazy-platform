namespace MazyPlatform.Scenario.Nodes.Data;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла получения записи.
/// </summary>
public sealed class GetRecordNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "get_record";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "recordIdVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной с ID записи для загрузки."),
        new(
            "recordVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для сохранения данных записи."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new GetRecordNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "recordIdVariable"),
            JsonParamHelper.GetRequiredString(parameters, "recordVariable"));
}
