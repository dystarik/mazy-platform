namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Контракт репозитория агрегата <see cref="UserAccount"/>.
/// </summary>
/// <remarks>
/// Расширяет базовый <see cref="IRepository{UserAccount}"/> методом поиска по email.
/// Реализация предоставляется инфраструктурным слоем.
/// </remarks>
public interface IUserAccountRepository : IRepository<UserAccount>
{
    /// <summary>
    /// Возвращает аккаунт пользователя по email-адресу.
    /// </summary>
    /// <param name="email">Email-адрес для поиска.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="UserAccount"/>, если аккаунт найден;
    /// <see langword="null"/> в противном случае.
    /// </returns>
    Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает аккаунт пользователя по email-адресу только если email ещё не подтверждён.
    /// </summary>
    /// <param name="email">Email-адрес для поиска.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="UserAccount"/>, если найден аккаунт с неподтверждённым email;
    /// <see langword="null"/> в противном случае.
    /// </returns>
    Task<UserAccount?> GetByUnverifiedEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает аккаунт пользователя по данным внешнего провайдера.
    /// </summary>
    /// <param name="externalProvider">Данные внешнего провайдера (тип и email).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="UserAccount"/>, если аккаунт найден;
    /// <see langword="null"/> в противном случае.
    /// </returns>
    Task<UserAccount?> GetByExternalProviderAsync(ExternalProvider externalProvider, CancellationToken cancellationToken = default);
}
