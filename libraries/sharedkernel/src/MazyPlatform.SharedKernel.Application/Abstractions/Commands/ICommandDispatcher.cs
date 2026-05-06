namespace MazyPlatform.SharedKernel.Application.Abstractions.Commands;

using MazyPlatform.SharedKernel.Domain.Results;

/// <summary>
/// Диспетчер команд: отвечает за передачу команд соответствующим обработчикам и получение результата выполнения.
/// Выполнение асинхронное; результаты оборачиваются в <see cref="Result"/> или <see cref="Result{TValue}"/>.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Отправляет команду без возвращаемого значения на обработку.
    /// </summary>
    /// <typeparam name="TCommand">Тип команды (реализует <see cref="ICommand"/>).</typeparam>
    /// <param name="command">Команда для выполнения. Не должна быть <see langword="null"/>.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Задача, представляющая асинхронную операцию. Результат — <see cref="Result"/>,
    /// содержащий признак успеха или набор ошибок при неудаче.
    /// </returns>
    Task<Result> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Отправляет команду, возвращающую ответ типа <typeparamref name="TResponse"/>, на обработку.
    /// </summary>
    /// <typeparam name="TCommand">Тип команды (реализует <see cref="ICommand{TResponse}"/>).</typeparam>
    /// <typeparam name="TResponse">Тип возвращаемого ответа (не допускается <see langword="null"/>).</typeparam>
    /// <param name="command">Команда для выполнения. Не должна быть <see langword="null"/>.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Задача, представляющая асинхронную операцию. Результат — <see cref="Result{TResponse}"/>,
    /// содержащий значение при успешном выполнении или набор ошибок при неудаче.
    /// </returns>
    Task<Result<TResponse>> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
        where TResponse : notnull;
}
