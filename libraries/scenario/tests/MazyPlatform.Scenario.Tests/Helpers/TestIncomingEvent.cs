namespace MazyPlatform.Scenario.Tests.Helpers;

using MazyPlatform.Scenario.Abstractions.Events;

public sealed class TestIncomingEvent : IIncomingEvent
{
    public IncomingEventType EventType { get; init; }

    public Guid BotId { get; init; }

    public string PlatformUserId { get; init; } = "test_user";

    public string ChatId { get; init; } = "test_chat";

    public string? Text { get; init; }

    public string? Payload { get; init; }

    public string? ImageUrl { get; init; }

    public string? ImageCaption { get; init; }

    public string? MessageId { get; init; }
}
