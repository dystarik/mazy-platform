namespace MazyPlatform.Service.Bot.Integration.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Long polling timeout for Telegram getUpdates.</summary>
    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Delay before retrying Telegram polling after a transient error.</summary>
    [Range(1, 60)]
    public int RetryDelaySeconds { get; init; } = 5;
}
