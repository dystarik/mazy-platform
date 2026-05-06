namespace MazyPlatform.Service.Notification.Messaging.Abstractions;

/// <summary>
/// Сервис отправки электронных писем.
/// </summary>
internal interface IEmailSender
{
    /// <summary>
    /// Асинхронно отправляет электронное письмо.
    /// </summary>
    /// <param name="to">Адрес получателя.</param>
    /// <param name="subject">Тема письма.</param>
    /// <param name="body">Тело письма.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see cref="Task"/> представляющая асинхронную операцию отправки.</returns>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
