namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

/// <summary>
/// Репозиторий агрегата <see cref="BotInstance"/>.
/// </summary>
public interface IBotInstanceRepository : IRepository<BotInstance>
{
    /// <summary>
    /// Возвращает экземпляр бота по идентификатору и идентификатору владельца.
    /// </summary>
    /// <param name="id">Идентификатор экземпляра бота.</param>
    /// <param name="ownerAccountId">Идентификатор аккаунта владельца.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// Экземпляр бота, если найден и принадлежит указанному владельцу;
    /// иначе <see langword="null"/>.
    /// </returns>
    Task<BotInstance?> GetByIdAndOwnerAsync(Guid id, Guid ownerAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все экземпляры ботов, привязанных к указанному проекту.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список ботов проекта (пустой, если ботов нет).</returns>
    Task<IReadOnlyList<BotInstance>> GetAllByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}
