namespace MazyPlatform.Scenario.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла получения изображения.
/// </summary>
public sealed class ReceiveImageNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "receive_image";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "imageUrlVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для сохранения URL полученного изображения."),
        new(
            "captionVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения подписи к изображению."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new ReceiveImageNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "imageUrlVariable"),
            services.GetRequiredService<IReceiveImageUseCase>(),
            JsonParamHelper.GetOptionalString(parameters, "captionVariable"));
}
