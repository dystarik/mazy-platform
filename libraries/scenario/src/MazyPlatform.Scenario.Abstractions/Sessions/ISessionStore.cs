namespace MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Хранилище сессий диалогов.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Получает активную сессию или создаёт новую.
    /// </summary>
    /// <param name="botId">Идентификатор бота.</param>
    /// <param name="platformUserId">Идентификатор пользователя в мессенджере.</param>
    /// <param name="startNodeId">Стартовый узел для новой сессии.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Существующая активная сессия или новая сессия.</returns>
    Task<ISession> GetOrCreateAsync(
        Guid botId,
        string platformUserId,
        Guid startNodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет текущее состояние сессии.
    /// </summary>
    /// <param name="session">Сессия для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию сохранения.</returns>
    Task SaveAsync(ISession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сессию по идентификатору.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
