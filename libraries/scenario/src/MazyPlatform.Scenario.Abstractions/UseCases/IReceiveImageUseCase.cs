namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Проверяет, является ли входящее событие изображением,
/// и извлекает из него данные.
/// </summary>
public interface IReceiveImageUseCase
{
    /// <summary>
    /// Проверяет, подходит ли событие.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns>true, если событие содержит изображение.</returns>
    bool CanHandle(IIncomingEvent incomingEvent);

    /// <summary>
    /// Извлекает URL изображения из события.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns>URL или идентификатор изображения.</returns>
    string ExtractImageUrl(IIncomingEvent incomingEvent);

    /// <summary>
    /// Извлекает подпись к изображению.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns>Подпись или null.</returns>
    string? ExtractCaption(IIncomingEvent incomingEvent);
}
