namespace MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;

/// <summary>
/// Контракт репозитория агрегата <see cref="OneTimePassword"/>.
/// </summary>
/// <remarks>
/// Расширяет базовый <see cref="IRepository{OneTimePassword}"/> специализированным
/// методом поиска последнего активного OTP по аккаунту и типу.
/// Реализация предоставляется инфраструктурным слоем.
/// </remarks>
public interface IOneTimePasswordRepository : IRepository<OneTimePassword>
{
    /// <summary>
    /// Возвращает последний неверифицированный и неаннулированный OTP для указанного аккаунта и типа.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта пользователя.</param>
    /// <param name="type">Тип OTP для фильтрации.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Самый последний <see cref="OneTimePassword"/> с <see cref="OneTimePassword.IsVerified"/> и
    /// <see cref="OneTimePassword.IsInvalidated"/> равными <see langword="false"/>,
    /// если он существует; иначе <see langword="null"/>.
    /// </returns>
    Task<OneTimePassword?> GetLatestUnverifiedByUserAccountIdAsync(Guid userAccountId, OtpType type, CancellationToken cancellationToken = default);
}
