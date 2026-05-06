namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла отправки текстового сообщения.
/// </summary>
public sealed class SendMessageNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "send_message";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "text",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст сообщения. Можно использовать {переменные} из сессии."),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID отправленного сообщения."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new SendMessageNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "text"),
            services.GetRequiredService<ISendMessageUseCase>(),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
}
