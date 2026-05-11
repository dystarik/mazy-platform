namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

public sealed record QueueStats(int Ready, int Unacknowledged, int Total);
