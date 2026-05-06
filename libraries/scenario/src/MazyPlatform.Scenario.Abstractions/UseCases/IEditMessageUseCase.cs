namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Юзкейс редактирования отправленного сообщения на платформе.
/// </summary>
public interface IEditMessageUseCase
{
    /// <summary>
    /// Редактирует сообщение с указанным идентификатором.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="messageId">Идентификатор сообщения на платформе.</param>
    /// <param name="newText">Новый текст сообщения (или подпись для вложений).</param>
    /// <param name="buttons">
    /// Новая раскладка кнопок. Если null — кнопки не меняются, остаются прежними.
    /// </param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Действие редактирования сообщения.</returns>
    Task<IOutgoingAction> ExecuteAsync(
        ExecutionContext context,
        string messageId,
        string newText,
        ButtonLayout? buttons,
        CancellationToken cancellationToken = default);
}
