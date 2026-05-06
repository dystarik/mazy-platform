namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Value Object привязанного внешнего провайдера аккаунта.
/// </summary>
/// <remarks>
/// Используется внутри агрегата <see cref="UserAccount"/> в коллекции <see cref="UserLinkedProviders"/>.
/// Хранит только доменные значения: <see cref="Type"/> и <see cref="Email"/>.
/// </remarks>
/// <param name="Type">Тип внешнего провайдера аутентификации.</param>
/// <param name="Email">Email пользователя у внешнего провайдера.</param>
public sealed record class ExternalProvider(ExternalProviderType Type, Email Email);
