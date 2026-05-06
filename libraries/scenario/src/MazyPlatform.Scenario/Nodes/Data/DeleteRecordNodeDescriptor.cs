namespace MazyPlatform.Scenario.Nodes.Data;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла удаления записи.
/// </summary>
public sealed class DeleteRecordNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "delete_record";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "recordIdVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной с ID записи для удаления."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new DeleteRecordNode(id, JsonParamHelper.GetRequiredString(parameters, "recordIdVariable"));
}
