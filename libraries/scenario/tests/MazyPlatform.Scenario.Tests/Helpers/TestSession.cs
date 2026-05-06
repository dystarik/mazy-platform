namespace MazyPlatform.Scenario.Tests.Helpers;

using MazyPlatform.Scenario.Abstractions.Sessions;

public sealed class TestSession : ISession
{
    public Guid SessionId { get; init; } = Guid.NewGuid();

    public Guid BotId { get; init; } = Guid.NewGuid();

    public string PlatformUserId { get; init; } = "test_user";

    public Guid? CurrentNodeId { get; set; }

    public SessionState State { get; set; } = SessionState.Active;

    public IDictionary<string, object?> Variables { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
