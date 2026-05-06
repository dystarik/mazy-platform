namespace MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Описание карточки карусели VK.
/// </summary>
/// <param name="Title">Заголовок карточки.</param>
/// <param name="Description">Описание карточки.</param>
/// <param name="PhotoId">Идентификатор фото VK (например, "-123_456").</param>
/// <param name="Buttons">Кнопки карточки.</param>
public sealed record VkCarouselCard(
    string Title,
    string Description,
    string? PhotoId,
    IReadOnlyList<VkCarouselButton> Buttons);
