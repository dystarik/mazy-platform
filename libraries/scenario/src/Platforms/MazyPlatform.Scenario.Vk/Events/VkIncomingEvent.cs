namespace MazyPlatform.Scenario.Vk.Events;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Входящее событие от VK Callback API.
/// </summary>
public sealed class VkIncomingEvent : IIncomingEvent
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
    /// Оригинальный JSON от VK для доступа к специфичным данным.
    /// </summary>
    public string? RawJson { get; init; }
}
