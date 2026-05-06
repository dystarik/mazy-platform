namespace MazyPlatform.Scenario.Vk.Nodes;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла удаления VK reply-клавиатуры.
/// </summary>
public sealed class VkRemoveKeyboardNodeDescriptor : NodeDescriptorBase, IPlatformScopedNodeDescriptor
{
    /// <inheritdoc />
    public IReadOnlySet<string> PlatformKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ScenarioPlatformKeys.Vk,
    };

    /// <inheritdoc />
    public override string Type => "vk_remove_keyboard";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "text",
            NodeParamType.String,
            IsRequired: true,
            Description: "Текст сообщения, отправляемого вместе с удалением клавиатуры."),
        new(
            "messageIdVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения ID отправленного сообщения."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new VkRemoveKeyboardNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "text"),
            services.GetRequiredService<VkRemoveKeyboardUseCase>(),
            JsonParamHelper.GetOptionalString(parameters, "messageIdVariable"));
}
