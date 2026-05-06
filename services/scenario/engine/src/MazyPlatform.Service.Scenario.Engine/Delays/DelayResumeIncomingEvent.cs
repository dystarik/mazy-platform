namespace MazyPlatform.Service.Scenario.Engine.Delays;

using MazyPlatform.Scenario.Abstractions.Events;

internal sealed class DelayResumeIncomingEvent : IIncomingEvent
{
    public required Guid BotId { get; init; }

    public required string PlatformUserId { get; init; }

    public required string ChatId { get; init; }

    public IncomingEventType EventType => IncomingEventType.Message;

    public string? Text => null;

    public string? Payload => null;

    public string? ImageUrl => null;

    public string? ImageCaption => null;

    public string? MessageId => null;
}
