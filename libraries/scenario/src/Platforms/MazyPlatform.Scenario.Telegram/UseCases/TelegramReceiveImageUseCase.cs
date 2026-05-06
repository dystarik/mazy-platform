namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// Telegram-реализация приёма изображения.
/// </summary>
public sealed class TelegramReceiveImageUseCase : IReceiveImageUseCase
{
    /// <inheritdoc />
    public bool CanHandle(IIncomingEvent incomingEvent) => incomingEvent.EventType == IncomingEventType.Image;

    /// <inheritdoc />
    public string ExtractImageUrl(IIncomingEvent incomingEvent) => incomingEvent.ImageUrl ?? string.Empty;

    /// <inheritdoc />
    public string? ExtractCaption(IIncomingEvent incomingEvent) => incomingEvent.ImageCaption;
}
