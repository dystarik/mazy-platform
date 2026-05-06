namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Execution;

/// <summary>
/// Отправка изображения пользователю.
/// Реализуется для каждой платформы.
/// </summary>
public interface ISendImageUseCase
{
    /// <summary>
    /// Формирует действие отправки изображения.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="imageUrl">URL или идентификатор изображения.</param>
    /// <param name="caption">Подпись (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отправки с действием и опциональным идентификатором сообщения.</returns>
    Task<SendResult> ExecuteAsync(ExecutionContext context, string imageUrl, string? caption = null, CancellationToken cancellationToken = default);
}
