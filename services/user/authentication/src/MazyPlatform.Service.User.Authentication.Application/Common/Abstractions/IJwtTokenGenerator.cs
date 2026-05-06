namespace MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Интерфейс генерации JWT-токенов для пользователя.
/// </summary>
/// <remarks>
/// Используется при аутентификации для формирования access-токена
/// на основе идентификаторов пользователя, refresh-токена и электронной почты.
/// </remarks>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Генерирует JWT-токен для пользователя.
    /// </summary>
    /// <param name="userAccountId">Идентификатор учетной записи пользователя.</param>
    /// <param name="refreshTokenId">Идентификатор refresh-токена, связанного с сессией.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    /// <returns>Строковое представление JWT-токена.</returns>
    string Generate(Guid userAccountId, Guid refreshTokenId, Email email);
}
