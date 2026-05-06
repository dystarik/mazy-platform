namespace MazyPlatform.Scenario.Vk.Actions;

using MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: удалить reply-клавиатуру из VK-диалога,
/// отправив сообщение с пустой клавиатурой.
/// </summary>
/// <param name="Text">Текст сообщения, отправляемого вместе с удалением клавиатуры.</param>
public sealed record VkRemoveKeyboardAction(string Text) : IOutgoingAction;
