namespace MazyPlatform.Service.Scenario.Engine.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class DelayResumeOptions
{
    public const string SectionName = "DelayResume";

    public bool Enabled { get; init; } = true;

    [Range(1, 3600)]
    public int PollIntervalSeconds { get; init; } = 1;

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 20;

    [Range(1, 3600)]
    public int LockSeconds { get; init; } = 30;
}
