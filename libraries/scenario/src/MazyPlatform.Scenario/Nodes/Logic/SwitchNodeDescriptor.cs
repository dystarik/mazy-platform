namespace MazyPlatform.Scenario.Nodes.Logic;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Дескриптор узла switch.
/// </summary>
public sealed class SwitchNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "switch";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "variable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной, значение которой проверяется."),
        new(
            "cases",
            NodeParamType.ObjectList,
            IsRequired: true,
            Description: "Список соответствий: значение переменной → ключ ветки.",
            Fields:
            [
                new(
                    "value",
                    NodeParamType.String,
                    IsRequired: true,
                    Description: "Значение переменной для сравнения."),
                new(
                    "branchKey",
                    NodeParamType.String,
                    IsRequired: true,
                    Description: "Ключ ветки, по которой пойдёт выполнение при совпадении."),
            ]),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new SwitchNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "variable"),
            JsonParamHelper.ParseCases(parameters));
}
