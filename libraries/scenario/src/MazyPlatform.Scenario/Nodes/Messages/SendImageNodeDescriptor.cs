namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла отправки изображения.
/// </summary>
public sealed class SendImageNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "send_image";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "imageUrl",
            NodeParamType.String,
            IsRequired: true,
            Description: "URL изображения для отправки."),
        new(
            "caption",
            NodeParamType.String,
            IsRequired: false,
            Description: "Подпись под изображением (опционально)."),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID отправленного сообщения."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new SendImageNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "imageUrl"),
            services.GetRequiredService<ISendImageUseCase>(),
            JsonParamHelper.GetOptionalString(parameters, "caption"),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
}
