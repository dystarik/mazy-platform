namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Хранилище схем пользовательских сущностей.
/// </summary>
public interface ISchemaStore
{
    /// <summary>
    /// Получает snapshot схемы сущности по идентификатору проекта, версии сценария и имени сущности.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="scenarioVersion">Версия сценария.</param>
    /// <param name="entityName">Имя сущности.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Схема сущности или <see langword="null"/>, если схема не найдена.</returns>
    Task<EntitySchema?> GetAsync(
        Guid projectId,
        int scenarioVersion,
        string entityName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает схему сущности по идентификатору проекта и имени сущности.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="entityName">Имя сущности.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Схема сущности или <see langword="null"/>, если схема не найдена.</returns>
    Task<EntitySchema?> GetAsync(
        Guid projectId,
        string entityName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает все snapshot-ы схем сущностей для проекта и версии сценария.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="scenarioVersion">Версия сценария.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список схем сущностей проекта.</returns>
    Task<IReadOnlyList<EntitySchema>> GetAllAsync(
        Guid projectId,
        int scenarioVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает все схемы сущностей для проекта.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список схем сущностей проекта.</returns>
    Task<IReadOnlyList<EntitySchema>> GetAllAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
