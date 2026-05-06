namespace MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// Получение информации о пользователе мессенджера.
/// Реализуется для каждой платформы.
/// </summary>
public interface IGetUserInfoUseCase
{
    /// <summary>
    /// Получает информацию о пользователе.
    /// </summary>
    /// <param name="platformUserId">Идентификатор пользователя на платформе.</param>
    /// <param name="botToken">Токен бота для вызова платформенного API.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Информация о пользователе.</returns>
    Task<UserInfo> GetAsync(
        string platformUserId,
        string botToken,
        CancellationToken cancellationToken = default);
}
