namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

/// <summary>
/// Учётные данные бота платформы Telegram.
/// </summary>
public sealed record TelegramBotCredentials : IBotCredentials
{
    /// <summary>
    /// Инициализирует учётные данные Telegram-бота.
    /// </summary>
    /// <param name="accessToken">Токен Telegram-бота.</param>
    public TelegramBotCredentials(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        AccessToken = accessToken;
    }

    /// <inheritdoc />
    public PlatformType PlatformType => PlatformType.Telegram;

    /// <inheritdoc />
    public string AccessToken { get; }
}
