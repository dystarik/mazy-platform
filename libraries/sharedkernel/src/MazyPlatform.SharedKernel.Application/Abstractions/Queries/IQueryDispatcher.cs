namespace MazyPlatform.SharedKernel.Application.Abstractions.Queries;

using MazyPlatform.SharedKernel.Domain.Results;

/// <summary>
/// Диспетчер запросов: отвечает за передачу запросов соответствующим обработчикам и получение результата выполнения.
/// Запросы предназначены для чтения данных и не должны изменять состояние приложения.
/// Выполнение асинхронное; результаты оборачиваются в <see cref="Result{TValue}"/>.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Отправляет запрос, возвращающий ответ типа <typeparamref name="TResponse"/>, на обработку.
    /// </summary>
    /// <typeparam name="TQuery">Тип запроса (реализует <see cref="IQuery{TResponse}"/>).</typeparam>
    /// <typeparam name="TResponse">Тип возвращаемого ответа (не допускается <see langword="null"/>).</typeparam>
    /// <param name="query">Запрос для выполнения. Не должен быть <see langword="null"/>.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Задача, представляющая асинхронную операцию. Результат — <see cref="Result{TResponse}"/>,
    /// содержащий значение при успешном выполнении или набор ошибок при неудаче.
    /// </returns>
    Task<Result<TResponse>> DispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
        where TResponse : notnull;
}
