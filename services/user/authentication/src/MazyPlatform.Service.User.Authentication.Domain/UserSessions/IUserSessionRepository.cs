namespace MazyPlatform.Service.User.Authentication.Domain.UserSessions;

/// <summary>
/// Контракт репозитория агрегата <see cref="UserSession"/>.
/// </summary>
/// <remarks>
/// Расширяет базовый <see cref="IRepository{UserSession}"/> методами поиска
/// по идентификатору токена и по идентификатору аккаунта.
/// Реализация предоставляется инфраструктурным слоем.
/// </remarks>
public interface IUserSessionRepository : IRepository<UserSession>
{
    /// <summary>
    /// Возвращает сессию по идентификатору токена обновления.
    /// </summary>
    /// <param name="refreshTokenId">Идентификатор токена (<see cref="UserSession.RefreshTokenId"/>).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="UserSession"/>, если сессия найдена;
    /// <see langword="null"/> в противном случае.
    /// </returns>
    Task<UserSession?> GetByRefreshTokenIdAsync(Guid refreshTokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все сессии, принадлежащие указанному аккаунту.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Коллекция всех <see cref="UserSession"/> аккаунта, включая отозванные.
    /// </returns>
    Task<IReadOnlyCollection<UserSession>> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken = default);
}
