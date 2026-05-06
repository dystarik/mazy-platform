namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions;

/// <summary>
/// Контракт репозитория агрегата <see cref="MfaSession"/>.
/// </summary>
/// <remarks>
/// Расширяет базовый <see cref="IRepository{MfaSession}"/> специализированными методами
/// для сценариев, в которых сессию нужно искать с дополнительными ограничениями по владельцу или действию.
/// Реализация предоставляется инфраструктурным слоем.
/// </remarks>
public interface IMfaSessionRepository : IRepository<MfaSession>
{
    /// <summary>
    /// Возвращает MFA-сессию по идентификатору и идентификатору аккаунта.
    /// </summary>
    /// <param name="mfaSessionId">Идентификатор MFA-сессии.</param>
    /// <param name="userAccountId">Идентификатор аккаунта-владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="MfaSession"/>, если найдена запись, принадлежащая указанному аккаунту;
    /// <see langword="null"/> в противном случае.
    /// </returns>
    Task<MfaSession?> GetByIdAndUserAccountIdAsync(Guid mfaSessionId, Guid userAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает MFA-сессию для неаутентифицированного сценария по её идентификатору.
    /// </summary>
    /// <param name="mfaSessionId">Идентификатор MFA-сессии.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="MfaSession"/> с типом <see cref="MfaSessionAction.Login"/> или <see cref="MfaSessionAction.ResetPassword"/>, если найдена;
    /// <see langword="null"/> в противном случае.
    /// </returns>
    Task<MfaSession?> GetUnauthenticatedSessionByIdAsync(Guid mfaSessionId, CancellationToken cancellationToken = default);
}
