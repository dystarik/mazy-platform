namespace MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Описание кнопки карточки карусели VK.
/// </summary>
/// <param name="Label">Текст кнопки.</param>
/// <param name="Payload">Payload кнопки.</param>
/// <param name="Link">Ссылка (опционально, если указана — тип кнопки "open_link").</param>
public sealed record VkCarouselButton(string Label, string Payload, string? Link = null);
