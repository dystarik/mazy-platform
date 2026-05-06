namespace MazyPlatform.Scenario.Vk.Actions;

using MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: отправить сообщение с VK-клавиатурой.
/// </summary>
/// <param name="Text">Текст сообщения.</param>
/// <param name="KeyboardJson">JSON клавиатуры в формате VK.</param>
public sealed record VkSendKeyboardAction(string Text, string KeyboardJson) : IOutgoingAction;
