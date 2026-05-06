namespace MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Описание кнопки VK-клавиатуры.
/// </summary>
/// <param name="Label">Текст кнопки.</param>
/// <param name="Payload">Payload кнопки.</param>
/// <param name="Color">Цвет кнопки (primary, secondary, negative, positive).</param>
public sealed record VkButtonInfo(string Label, string Payload, string? Color = null);
