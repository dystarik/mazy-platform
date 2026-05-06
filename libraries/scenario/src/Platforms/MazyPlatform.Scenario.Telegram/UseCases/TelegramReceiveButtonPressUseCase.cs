namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// Telegram-реализация приёма нажатия inline-кнопки.
/// </summary>
public sealed class TelegramReceiveButtonPressUseCase : IReceiveButtonPressUseCase
{
    /// <inheritdoc />
    public bool CanHandle(IIncomingEvent incomingEvent) => incomingEvent.EventType == IncomingEventType.ButtonPress;

    /// <inheritdoc />
    public string ExtractPayload(IIncomingEvent incomingEvent) => incomingEvent.Payload ?? string.Empty;
}
