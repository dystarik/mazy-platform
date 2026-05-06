namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла ожидания нажатия кнопки.
/// </summary>
public sealed class ReceiveButtonPressNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "receive_button_press";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "buttonPayloadVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для сохранения payload нажатой кнопки."),
        new(
            "expectedPayloads",
            NodeParamType.StringList,
            IsRequired: false,
            Description: "Список ожидаемых payload кнопок (опционально)."),
        new(
            "isEntry",
            NodeParamType.Bool,
            IsRequired: false,
            Description: "Если включено — узел становится точкой входа в сценарий по этой кнопке."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new ReceiveButtonPressNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "buttonPayloadVariable"),
            services.GetRequiredService<IReceiveButtonPressUseCase>(),
            JsonParamHelper.GetOptionalStringList(parameters, "expectedPayloads"),
            JsonParamHelper.GetOptionalBool(parameters, "isEntry"));
}
