namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;

/// <summary>
/// Доменный сервис входа пользователя через внешний провайдер.
/// </summary>
/// <remarks>
/// Принимает данные внешнего провайдера как value object <see cref="ExternalProvider"/>,
/// содержащий тип провайдера и email пользователя.
/// Если аккаунт с указанным провайдером (тип + email) отсутствует — создаёт новый аккаунт через внешний провайдер.
/// </remarks>
public sealed class LoginByExternalProviderService(
    IUserAccountRepository userAccountRepository,
    ITokenHasher tokenHasher,
    TimeProvider timeProvider) : IDomainService
{
    /// <summary>
    /// Выполняет вход по внешнему провайдеру, при необходимости создавая новый аккаунт.
    /// </summary>
    /// <param name="externalProvider">
    /// Данные внешнего провайдера (тип и email), полученные после успешной авторизации у провайдера.
    /// </param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с <see cref="LoginByExternalProviderOutput"/>:
    /// успех — создана новая или открыта существующая сессия;
    /// сбой с кодом <see cref="ErrorCodes.Auth.Unauthorized"/> не ожидается в штатном сценарии.
    /// </returns>
    public async Task<Result<LoginByExternalProviderOutput>> ExecuteAsync(ExternalProvider externalProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(externalProvider);

        var now = timeProvider.GetUtcNow();

        var userAccount = await userAccountRepository.GetByExternalProviderAsync(externalProvider, cancellationToken);
        var isNewAccount = userAccount is null;
        userAccount ??= UserAccount.RegisterByExternalProvider(externalProvider.Email, externalProvider, now);

        var refreshToken = Guid.NewGuid().ToString();
        var refreshTokenHash = RefreshTokenHash.FromTrusted(tokenHasher.Hash(refreshToken));
        var userSession = UserSession.CreateNewSession(userAccount.Id, refreshTokenHash, now);

        return new LoginByExternalProviderOutput(userAccount, userSession, refreshToken, isNewAccount);
    }
}
