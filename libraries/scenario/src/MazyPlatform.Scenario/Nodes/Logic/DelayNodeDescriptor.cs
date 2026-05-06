namespace MazyPlatform.Scenario.Nodes.Logic;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла задержки.
/// </summary>
public sealed class DelayNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "delay";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "seconds",
            NodeParamType.Int,
            IsRequired: true,
            Description: "Длительность задержки в секундах."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new DelayNode(id, parameters.GetProperty("seconds").GetInt32());
}
