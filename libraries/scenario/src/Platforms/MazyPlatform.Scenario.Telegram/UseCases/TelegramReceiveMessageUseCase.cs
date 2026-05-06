namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// Telegram-реализация приёма текстового сообщения.
/// </summary>
public sealed class TelegramReceiveMessageUseCase : IReceiveMessageUseCase
{
    /// <inheritdoc />
    public bool CanHandle(IIncomingEvent incomingEvent) => incomingEvent.EventType == IncomingEventType.Message;

    /// <inheritdoc />
    public string ExtractText(IIncomingEvent incomingEvent) => incomingEvent.Text ?? string.Empty;
}
