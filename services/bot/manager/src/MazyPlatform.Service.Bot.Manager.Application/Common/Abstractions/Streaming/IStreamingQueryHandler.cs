namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;

/// <summary>
/// Обработчик streaming-запроса.
/// </summary>
/// <typeparam name="TQuery">Тип запроса.</typeparam>
/// <typeparam name="TItem">Тип элемента в потоке.</typeparam>
public interface IStreamingQueryHandler<in TQuery, TItem>
    where TQuery : IStreamingQuery<TItem>
{
    /// <summary>
    /// Обрабатывает запрос и возвращает асинхронный поток элементов.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Асинхронный поток элементов типа <typeparamref name="TItem"/>.</returns>
    IAsyncEnumerable<TItem> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
