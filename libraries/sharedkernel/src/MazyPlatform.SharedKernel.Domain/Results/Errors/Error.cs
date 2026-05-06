namespace MazyPlatform.SharedKernel.Domain.Results.Errors;

/// <summary>
/// Ошибка доменной операции.
/// </summary>
/// <example>
/// <code>
/// // Создание ошибок через фабричные методы
/// var validationError = Error.Validation("Email.Invalid", "Неверный формат email");
/// var notFoundError = Error.NotFound("User.NotFound", "Пользователь не найден");
/// var conflictError = Error.Conflict("User.Exists", "Пользователь уже существует");
/// </code>
/// </example>
public sealed record class Error
{
    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>
    /// Код ошибки для программной идентификации.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Описание ошибки для пользователя.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Категория ошибки.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Создаёт ошибку валидации.
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <returns>
    /// Экземпляр <see cref="Error"/> с <see cref="Type"/> равным <see cref="ErrorType.Validation"/>.
    /// </returns>
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    /// <summary>
    /// Создаёт ошибку "не найдено".
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <returns>
    /// Экземпляр <see cref="Error"/> с <see cref="Type"/> равным <see cref="ErrorType.NotFound"/>.
    /// </returns>
    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Создаёт ошибку авторизации.
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <returns>
    /// Экземпляр <see cref="Error"/> с <see cref="Type"/> равным <see cref="ErrorType.Unauthorized"/>.
    /// </returns>
    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Создаёт ошибку конфликта.
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <returns>
    /// Экземпляр <see cref="Error"/> с <see cref="Type"/> равным <see cref="ErrorType.Conflict"/>.
    /// </returns>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Создаёт внутреннюю ошибку.
    /// </summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <returns>
    /// Экземпляр <see cref="Error"/> с <see cref="Type"/> равным <see cref="ErrorType.Internal"/>.
    /// </returns>
    public static Error Internal(string code, string message) =>
        new(code, message, ErrorType.Internal);
}
