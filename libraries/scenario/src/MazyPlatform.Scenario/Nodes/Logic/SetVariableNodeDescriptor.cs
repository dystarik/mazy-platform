namespace MazyPlatform.Scenario.Nodes.Logic;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла установки переменной.
/// </summary>
public sealed class SetVariableNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "set_variable";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "variable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для записи."),
        new(
            "value",
            NodeParamType.String,
            IsRequired: true,
            Description: "Значение, можно использовать {шаблоны} из сессии."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new SetVariableNode(id, JsonParamHelper.GetRequiredString(parameters, "variable"), JsonParamHelper.GetRequiredString(parameters, "value"));
}
