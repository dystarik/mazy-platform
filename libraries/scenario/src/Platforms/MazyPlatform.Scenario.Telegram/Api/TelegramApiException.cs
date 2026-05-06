namespace MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Исключение, возникающее при ошибке Telegram Bot API.
/// </summary>
public sealed class TelegramApiException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр с кодом и описанием ошибки Telegram.
    /// </summary>
    /// <param name="errorCode">Код ошибки Telegram.</param>
    /// <param name="description">Описание ошибки Telegram.</param>
    public TelegramApiException(int errorCode, string description)
        : base($"Telegram API error {errorCode}: {description}")
    {
        ErrorCode = errorCode;
        Description = description;
    }

    /// <summary>
    /// Инициализирует новый экземпляр с сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public TelegramApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр с сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public TelegramApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр без параметров.
    /// </summary>
    public TelegramApiException()
    {
    }

    /// <summary>
    /// Код ошибки Telegram.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Описание ошибки Telegram.
    /// </summary>
    public string Description { get; } = string.Empty;
}
