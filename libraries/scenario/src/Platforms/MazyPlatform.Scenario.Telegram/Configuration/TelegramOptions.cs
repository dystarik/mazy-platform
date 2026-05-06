namespace MazyPlatform.Scenario.Telegram.Configuration;

/// <summary>
/// Настройки Telegram Bot API.
/// </summary>
public sealed class TelegramOptions
{
    /// <summary>
    /// Базовый URL Telegram Bot API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.telegram.org";

    /// <summary>
    /// Базовый URL для скачивания файлов Telegram.
    /// </summary>
    public string FileBaseUrl { get; set; } = "https://api.telegram.org/file";
}
