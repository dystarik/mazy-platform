namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;

/// <summary>
/// Результат успешного входа через внешний провайдер.
/// </summary>
/// <param name="UserAccount">Аккаунт пользователя (новый или существующий).</param>
/// <param name="UserSession">Новая пользовательская сессия для сохранения в хранилище.</param>
/// <param name="RefreshToken">Открытый refresh-токен для передачи клиенту.</param>
/// <param name="IsNewAccount">Признак того, что аккаунт был создан в результате входа через внешний провайдер.</param>
public sealed record LoginByExternalProviderOutput(UserAccount UserAccount, UserSession UserSession, string RefreshToken, bool IsNewAccount);
