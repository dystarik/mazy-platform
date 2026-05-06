namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Отправка текстового сообщения пользователю.
/// Реализуется для каждой платформы (VK, Telegram).
/// </summary>
public interface ISendMessageUseCase
{
    /// <summary>
    /// Формирует исходящее действие отправки текстового сообщения.
    /// </summary>
    /// <param name="context">Контекст выполнения сценария.</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отправки с действием и опциональным идентификатором сообщения.</returns>
    Task<SendResult> ExecuteAsync(ExecutionContext context, string text, CancellationToken cancellationToken = default);
}
