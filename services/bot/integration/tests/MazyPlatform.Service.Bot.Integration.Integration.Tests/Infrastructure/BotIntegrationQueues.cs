namespace MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

internal static class BotIntegrationQueues
{
    public const string Activated = "bot-integration.bot-activated";
    public const string Deactivated = "bot-integration.bot-deactivated";
    public const string Deleted = "bot-integration.bot-deleted";
    public const string TokenChanged = "bot-integration.bot-token-changed";
    public const string VersionChanged = "bot-integration.bot-version-changed";
    public const string Unbound = "bot-integration.bot-unbound";

    public static readonly IReadOnlyList<string> Main =
    [
        Activated,
        Deactivated,
        Deleted,
        TokenChanged,
        VersionChanged,
        Unbound,
    ];

    public static string DeadLetter(string queueName) => $"{queueName}.dlx";
}
