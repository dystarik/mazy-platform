namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Юзкейс удаления отправленного сообщения на платформе.
/// </summary>
public interface IDeleteMessageUseCase
{
    /// <summary>
    /// Удаляет сообщение с указанным идентификатором.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="messageId">Идентификатор сообщения на платформе.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Действие удаления сообщения.</returns>
    Task<IOutgoingAction> ExecuteAsync(ExecutionContext context, string messageId, CancellationToken cancellationToken = default);
}
