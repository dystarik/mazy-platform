namespace MazyPlatform.Scenario.Telegram.Events;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Входящее событие от Telegram webhook.
/// </summary>
public sealed class TelegramIncomingEvent : IIncomingEvent
{
    /// <inheritdoc />
    public required IncomingEventType EventType { get; init; }

    /// <inheritdoc />
    public required Guid BotId { get; init; }

    /// <inheritdoc />
    public required string PlatformUserId { get; init; }

    /// <inheritdoc />
    public required string ChatId { get; init; }

    /// <inheritdoc />
    public string? Text { get; init; }

    /// <inheritdoc />
    public string? Payload { get; init; }

    /// <inheritdoc />
    public string? ImageUrl { get; init; }

    /// <inheritdoc />
    public string? ImageCaption { get; init; }

    /// <inheritdoc />
    public string? MessageId { get; init; }

    /// <summary>
    /// Идентификатор callback_query для Telegram.
    /// </summary>
    public string? CallbackQueryId { get; init; }

    /// <summary>
    /// Оригинальный JSON от Telegram.
    /// </summary>
    public string? RawJson { get; init; }
}
