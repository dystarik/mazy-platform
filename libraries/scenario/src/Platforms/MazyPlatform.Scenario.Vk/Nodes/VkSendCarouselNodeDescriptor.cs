namespace MazyPlatform.Scenario.Vk.Nodes;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;
using MazyPlatform.Scenario.Vk.Helpers;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла отправки VK-карусели.
/// </summary>
public sealed class VkSendCarouselNodeDescriptor : NodeDescriptorBase, IPlatformScopedNodeDescriptor
{
    /// <inheritdoc />
    public IReadOnlySet<string> PlatformKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ScenarioPlatformKeys.Vk,
    };

    /// <inheritdoc />
    public override string Type => "vk_send_carousel";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "text",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст сообщения над каруселью."),
        new(
            "cards",
            NodeParamType.ObjectList,
            IsRequired: true,
            Description: "Список карточек карусели.",
            Fields:
            [
                new(
                    "title",
                    NodeParamType.String,
                    IsRequired: true,
                    Description: "Заголовок карточки."),
                new(
                    "description",
                    NodeParamType.String,
                    IsRequired: true,
                    Description: "Описание карточки."),
                new(
                    "photoId",
                    NodeParamType.String,
                    IsRequired: false,
                    Description: "ID фото VK для карточки (опционально)."),
                new(
                    "buttons",
                    NodeParamType.ObjectList,
                    IsRequired: false,
                    Description: "Кнопки карточки.",
                    Fields: ButtonSchemas.CarouselButton),
            ]),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID отправленного сообщения."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new VkSendCarouselNode(
            id,
            VkJsonParamHelper.ParseCarouselCards(parameters),
            services.GetRequiredService<VkSendCarouselUseCase>(),
            JsonParamHelper.GetRequiredString(parameters, "text"),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
}
