namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла отправки кнопок.
/// </summary>
public sealed class SendButtonsNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "send_buttons";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "text",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст сообщения над кнопками."),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID отправленного сообщения."),
        new(
            "buttons",
            NodeParamType.ObjectMatrix,
            IsRequired: true,
            Description: "Двумерный массив кнопок: ряды × кнопки в ряду.",
            Fields: ButtonSchemas.MessageButton),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new SendButtonsNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "text"),
            JsonParamHelper.ParseButtonLayout(parameters),
            services.GetRequiredService<ISendButtonsUseCase>(),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
}
