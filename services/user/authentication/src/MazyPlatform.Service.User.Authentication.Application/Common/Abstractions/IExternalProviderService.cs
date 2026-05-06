namespace MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Интерфейс сервиса для взаимодействия с внешними провайдерами аутентификации.
/// </summary>
/// <remarks>
/// Используется для получения данных пользователя по коду авторизации,
/// выданному внешним провайдером.
/// </remarks>
public interface IExternalProviderService
{
    /// <summary>
    /// Асинхронно получает адрес электронной почты пользователя от внешнего провайдера.
    /// </summary>
    /// <param name="providerType">Тип внешнего провайдера аутентификации.</param>
    /// <param name="code">Код авторизации, полученный от внешнего провайдера.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Экземпляр <see cref="Email"/>, если почта была успешно получена; иначе <see langword="null"/>.
    /// </returns>
    public Task<Email?> GetEmailAsync(ExternalProviderType providerType, string code, CancellationToken cancellationToken);
}
