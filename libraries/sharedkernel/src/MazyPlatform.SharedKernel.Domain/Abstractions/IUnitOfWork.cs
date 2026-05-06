namespace MazyPlatform.SharedKernel.Domain.Abstractions;

/// <summary>
/// Unit of Work — единица работы, координирующая изменения, выполненные через репозитории,
/// и фиксирующая их одним сохранением (как правило, в рамках транзакции).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Асинхронно фиксирует все изменения, накопленные в текущей единице работы, в хранилище данных.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Task"/>, представляющая асинхронную операцию сохранения.
    /// </returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
