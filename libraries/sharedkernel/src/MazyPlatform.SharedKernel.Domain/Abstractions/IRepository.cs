namespace MazyPlatform.SharedKernel.Domain.Abstractions;

using MazyPlatform.SharedKernel.Domain.Primitives;

/// <summary>
/// Репозиторий для работы с агрегатами типа <typeparamref name="TEntity"/>.
/// Определяет операции добавления, обновления, удаления и выборки агрегатов из хранилища.
/// </summary>
/// <typeparam name="TEntity">Тип агрегата, наследующий <see cref="AggregateRoot"/>.</typeparam>
public interface IRepository<TEntity>
    where TEntity : AggregateRoot
{
    /// <summary>
    /// Добавляет агрегат в контекст/единицу работы для последующего сохранения.
    /// </summary>
    /// <param name="entity">Агрегат, который необходимо добавить.</param>
    /// <remarks>
    /// Метод не выполняет сохранение в хранилище — фиксация изменений производится на уровне Unit of Work.
    /// </remarks>
    void Add(TEntity entity);

    /// <summary>
    /// Отмечает агрегат как изменённый для последующего сохранения.
    /// </summary>
    /// <param name="entity">Агрегат, который необходимо обновить.</param>
    /// <remarks>
    /// Метод не выполняет сохранение в хранилище — фиксация изменений производится на уровне Unit of Work.
    /// </remarks>
    void Update(TEntity entity);

    /// <summary>
    /// Отмечает агрегат для удаления из хранилища при последующей фиксации изменений.
    /// </summary>
    /// <param name="entity">Агрегат, который необходимо удалить.</param>
    /// <remarks>
    /// Метод не выполняет удаление немедленно — фактическое удаление происходит при сохранении Unit of Work.
    /// </remarks>
    void Delete(TEntity entity);

    /// <summary>
    /// Получает агрегат по уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор агрегата.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Task{TResult}"/>, результатом которой является найденный агрегат
    /// или <see langword="null"/>, если агрегат с указанным идентификатором отсутствует.
    /// </returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
