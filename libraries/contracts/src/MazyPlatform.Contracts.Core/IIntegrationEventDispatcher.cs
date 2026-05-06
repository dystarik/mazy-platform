namespace MazyPlatform.Contracts.Core;

/// <summary>
/// Диспетчер интеграционных событий.
/// По ключу маршрутизации находит нужный обработчик и передаёт ему событие.
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Асинхронно диспетчеризует событие по ключу маршрутизации.
    /// </summary>
    /// <param name="routingKey">Ключ маршрутизации из брокера сообщений.</param>
    /// <param name="json">Тело сообщения в формате JSON.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see cref="Task"/>, представляющая асинхронную операцию диспетчеризации.</returns>
    Task DispatchAsync(string routingKey, string json, CancellationToken cancellationToken = default);
}
