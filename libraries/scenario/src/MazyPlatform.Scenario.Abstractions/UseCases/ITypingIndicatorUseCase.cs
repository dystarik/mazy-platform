namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Показывает индикатор "бот печатает..." в чате.
/// Реализуется для каждой платформы.
/// </summary>
public interface ITypingIndicatorUseCase
{
    /// <summary>
    /// Формирует действие показа индикатора набора текста.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Действие для показа индикатора.</returns>
    Task<IOutgoingAction> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default);
}
