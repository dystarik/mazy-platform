namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Хранилище пользовательских данных (записи сущностей).
/// </summary>
public interface IDataStore
{
    /// <summary>
    /// Создаёт новую запись в области видимости текущего пользователя бота.
    /// </summary>
    /// <param name="schemaSnapshotId">Идентификатор snapshot-а схемы.</param>
    /// <param name="data">Данные записи.</param>
    /// <param name="scope">Область видимости данных.</param>
    /// <param name="sessionId">Привязка к сессии (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Созданная запись.</returns>
    Task<EntityRecord> CreateAsync(
        Guid schemaSnapshotId,
        IReadOnlyDictionary<string, object?> data,
        DataScope scope,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт новую запись.
    /// </summary>
    /// <param name="schemaId">Идентификатор схемы.</param>
    /// <param name="data">Данные записи.</param>
    /// <param name="sessionId">Привязка к сессии (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Созданная запись.</returns>
    Task<EntityRecord> CreateAsync(
        Guid schemaId,
        IReadOnlyDictionary<string, object?> data,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает запись по идентификатору в области видимости текущего пользователя бота.
    /// </summary>
    /// <param name="recordId">Идентификатор записи.</param>
    /// <param name="scope">Область видимости данных.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Запись или null.</returns>
    Task<EntityRecord?> GetAsync(
        Guid recordId,
        DataScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает запись по идентификатору.
    /// </summary>
    /// <param name="recordId">Идентификатор записи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Запись или null.</returns>
    Task<EntityRecord?> GetAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Частично обновляет данные записи в области видимости текущего пользователя бота.
    /// </summary>
    /// <param name="recordId">Идентификатор записи.</param>
    /// <param name="data">Поля, которые нужно обновить.</param>
    /// <param name="scope">Область видимости данных.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию обновления.</returns>
    Task UpdateAsync(
        Guid recordId,
        IReadOnlyDictionary<string, object?> data,
        DataScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет данные записи.
    /// </summary>
    /// <param name="recordId">Идентификатор записи.</param>
    /// <param name="data">Новые данные.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
    Task UpdateAsync(
        Guid recordId,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Находит записи текущего пользователя бота по фильтру.
    /// </summary>
    /// <param name="schemaId">Стабильный идентификатор схемы.</param>
    /// <param name="scope">Область видимости данных.</param>
    /// <param name="filter">Фильтр по полям (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список найденных записей.</returns>
    Task<IReadOnlyList<EntityRecord>> QueryAsync(
        Guid schemaId,
        DataScope scope,
        IReadOnlyDictionary<string, object?>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Находит записи по фильтру.
    /// </summary>
    /// <param name="schemaId">Идентификатор схемы.</param>
    /// <param name="filter">Фильтр по полям (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список найденных записей.</returns>
    Task<IReadOnlyList<EntityRecord>> QueryAsync(
        Guid schemaId,
        IReadOnlyDictionary<string, object?>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает связанную запись по значению поля-ссылки.
    /// </summary>
    /// <param name="referenceRecordId">Идентификатор связанной записи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Связанная запись или null.</returns>
    Task<EntityRecord?> GetReferencedAsync(
        Guid referenceRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет запись в области видимости текущего пользователя бота.
    /// </summary>
    /// <param name="recordId">Идентификатор записи.</param>
    /// <param name="scope">Область видимости данных.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
    Task DeleteAsync(
        Guid recordId,
        DataScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет запись.
    /// </summary>
    /// <param name="recordId">Идентификатор записи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
    Task DeleteAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);
}
