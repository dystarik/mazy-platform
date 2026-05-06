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
/// Дескриптор узла отправки VK-клавиатуры.
/// </summary>
public sealed class VkSendKeyboardNodeDescriptor : NodeDescriptorBase, IPlatformScopedNodeDescriptor
{
    /// <inheritdoc />
    public IReadOnlySet<string> PlatformKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ScenarioPlatformKeys.Vk,
    };

    /// <inheritdoc />
    public override string Type => "vk_send_keyboard";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "text",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст сообщения над клавиатурой."),
        new(
            "buttons",
            NodeParamType.ObjectMatrix,
            IsRequired: true,
            Description: "Двумерный массив кнопок: ряды × кнопки в ряду.",
            Fields:
            [
                new(
                    "label",
                    NodeParamType.String,
                    IsRequired: true,
                    Description: "Текст на кнопке."),
                new(
                    "payload",
                    NodeParamType.String,
                    IsRequired: true,
                    Description: "Внутренний идентификатор для обработки нажатия."),
                new(
                    "color",
                    NodeParamType.Enum,
                    IsRequired: false,
                    Description: "Цвет кнопки.",
                    EnumValues: ["primary", "secondary", "negative", "positive"]),
            ]),
        new(
            "oneTime",
            NodeParamType.Bool,
            IsRequired: false,
            Description: "Скрывать клавиатуру после нажатия."),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID отправленного сообщения."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new VkSendKeyboardNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "text"),
            VkJsonParamHelper.ParseKeyboardButtons(parameters),
            JsonParamHelper.GetOptionalBool(parameters, "oneTime"),
            services.GetRequiredService<VkSendKeyboardUseCase>(),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
}
