namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Отправка сообщения с кнопками.
/// </summary>
public interface ISendButtonsUseCase
{
    /// <summary>
    /// Формирует исходящее действие отправки сообщения с кнопками.
    /// </summary>
    /// <param name="context">Контекст выполнения сценария.</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="buttons">Раскладка кнопок для отправки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отправки с действием и опциональным идентификатором сообщения.</returns>
    Task<SendResult> ExecuteAsync(
        ExecutionContext context,
        string text,
        ButtonLayout buttons,
        CancellationToken cancellationToken = default);
}
