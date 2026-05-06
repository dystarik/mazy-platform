namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;

/// <summary>
/// Маркерный интерфейс streaming-запроса.
/// </summary>
/// <typeparam name="TItem">Тип элемента, возвращаемого в потоке.</typeparam>
/// <remarks>
/// Используется для CQRS-запросов, результатом которых является поток элементов
/// (<see cref="IAsyncEnumerable{T}"/>), а не одиночный ответ.
/// </remarks>
public interface IStreamingQuery<out TItem>
{
}
