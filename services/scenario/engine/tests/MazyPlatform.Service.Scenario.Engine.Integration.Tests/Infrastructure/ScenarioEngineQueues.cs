namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

internal static class ScenarioEngineQueues
{
    public const string Activated = "scenario-engine.bot-activated";
    public const string BotIncoming = "scenario-engine.bot-incoming";
    public const string Deactivated = "scenario-engine.bot-deactivated";
    public const string Deleted = "scenario-engine.bot-deleted";
    public const string TokenChanged = "scenario-engine.bot-token-changed";
    public const string Unbound = "scenario-engine.bot-unbound";
    public const string VersionChanged = "scenario-engine.bot-version-changed";

    public static readonly IReadOnlyList<string> Main =
    [
        BotIncoming,
        Activated,
        Deactivated,
        Deleted,
        TokenChanged,
        Unbound,
        VersionChanged,
    ];

    public static string DeadLetter(string queueName) => $"{queueName}.dlx";
}
