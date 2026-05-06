namespace MazyPlatform.Scenario.Nodes.Logic;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла условия.
/// </summary>
public sealed class ConditionNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "condition";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "leftOperand",
            NodeParamType.String,
            IsRequired: true,
            Description: "Левая часть сравнения. Можно использовать {переменные} из сессии."),
        new(
            "operator",
            NodeParamType.Enum,
            IsRequired: true,
            Description: "Оператор сравнения.",
            EnumValues: ["==", "!=", ">", "<", ">=", "<=", "contains", "startsWith"]),
        new(
            "rightOperand",
            NodeParamType.String,
            IsRequired: true,
            Description: "Правая часть сравнения. Можно использовать {переменные} из сессии."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new ConditionNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "leftOperand"),
            JsonParamHelper.GetRequiredString(parameters, "operator"),
            JsonParamHelper.GetRequiredString(parameters, "rightOperand"));
}
