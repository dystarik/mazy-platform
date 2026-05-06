namespace MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// Исключение, возникающее при ошибке VK API.
/// </summary>
public sealed class VkApiException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр с кодом и описанием ошибки VK.
    /// </summary>
    /// <param name="errorCode">Код ошибки VK.</param>
    /// <param name="errorMessage">Описание ошибки от VK.</param>
    public VkApiException(int errorCode, string errorMessage)
        : base($"VK API error {errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Инициализирует новый экземпляр с сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public VkApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр с сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public VkApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр без параметров.
    /// </summary>
    public VkApiException()
    {
    }

    /// <summary>
    /// Код ошибки VK.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Описание ошибки от VK.
    /// </summary>
    public string ErrorMessage { get; } = string.Empty;
}
