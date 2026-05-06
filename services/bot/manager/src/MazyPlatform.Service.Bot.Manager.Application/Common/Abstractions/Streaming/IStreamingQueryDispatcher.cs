namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;

/// <summary>
/// Диспатчер streaming-запросов.
/// </summary>
/// <remarks>
/// Резолвит соответствующий <see cref="IStreamingQueryHandler{TQuery,TItem}"/> из DI и проксирует вызов.
/// </remarks>
public interface IStreamingQueryDispatcher
{
    /// <summary>
    /// Диспатчит streaming-запрос к соответствующему обработчику.
    /// </summary>
    /// <typeparam name="TQuery">Тип запроса.</typeparam>
    /// <typeparam name="TItem">Тип элемента в потоке.</typeparam>
    /// <param name="query">Запрос.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Асинхронный поток элементов типа <typeparamref name="TItem"/>.</returns>
    IAsyncEnumerable<TItem> DispatchAsync<TQuery, TItem>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IStreamingQuery<TItem>;
}
