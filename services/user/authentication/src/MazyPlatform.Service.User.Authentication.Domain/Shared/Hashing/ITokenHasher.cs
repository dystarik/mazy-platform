namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Определяет контракт для хэширования и верификации токенов обновления сессий.
/// </summary>
/// <remarks>
/// Используется доменными сервисами (<see cref="LoginByPasswordService"/>, <see cref="CompleteRegistrationService"/>)
/// при выдаче нового <see cref="UserSession"/> и в <see cref="UserSession.RefreshSession"/>
/// при проверке переданного клиентом токена.
/// </remarks>
public interface ITokenHasher
{
    /// <summary>
    /// Вычисляет хэш токена обновления.
    /// </summary>
    /// <param name="token">Открытый токен для хэширования.</param>
    /// <returns>Строка хэша для сохранения в <see cref="RefreshTokenHash"/>.</returns>
    string Hash(string token);

    /// <summary>
    /// Проверяет соответствие открытого токена ранее вычисленному хэшу.
    /// </summary>
    /// <param name="token">Открытый токен, переданный клиентом.</param>
    /// <param name="tokenHash">Хэш токена из базы данных.</param>
    /// <returns><see langword="true"/>, если токен совпадает; иначе <see langword="false"/>.</returns>
    bool Verify(string token, string tokenHash);
}
