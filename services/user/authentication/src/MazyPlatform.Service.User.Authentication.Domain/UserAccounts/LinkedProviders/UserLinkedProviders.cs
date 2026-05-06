namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Сущность, агрегирующая привязанные внешние провайдеры аккаунта.
/// </summary>
/// <remarks>
/// Дочерняя сущность агрегата <see cref="UserAccount"/>.
/// Управляет только коллекцией Value Object <see cref="ExternalProvider"/> и не отвечает за аутентификацию.
/// Тип провайдера задаётся через <see cref="ExternalProviderType"/>.
/// Мутирующие методы доступны только внутри сборки домена и вызываются через <c>UserAccount</c>.
/// </remarks>
/// <seealso cref="ExternalProvider"/>
/// <seealso cref="ExternalProviderType"/>
public sealed class UserLinkedProviders : Entity
{
    private readonly List<ExternalProvider> _linkedProviders = [];

    private UserLinkedProviders() { }

    /// <summary>
    /// Идентификатор родительского аккаунта.
    /// </summary>
    public Guid UserAccountId { get; init; }

    /// <summary>
    /// Все привязанные внешние провайдеры аккаунта.
    /// </summary>
    public IReadOnlyCollection<ExternalProvider> LinkedProviders => _linkedProviders.AsReadOnly();

    /// <summary>
    /// Создаёт пустой набор привязок внешних провайдеров для указанного аккаунта.
    /// </summary>
    /// <param name="userAccountId">Идентификатор родительского аккаунта.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>Новый экземпляр <see cref="UserLinkedProviders"/> без привязанных провайдеров.</returns>
    public static UserLinkedProviders Create(Guid userAccountId, DateTimeOffset now)
    {
        return new UserLinkedProviders
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Привязывает внешний провайдер к аккаунту.
    /// </summary>
    /// <param name="provider">Value Object внешнего провайдера с типом и email.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если провайдер привязан;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.ExternalProviderAlreadyLinked"/>, если провайдер уже привязан.
    /// </returns>
    internal Result LinkProvider(ExternalProvider provider, DateTimeOffset now)
    {
        if (_linkedProviders.Exists(x => x.Type == provider.Type))
            return Error.Conflict(ErrorCodes.Auth.UserAccount.ExternalProviderAlreadyLinked, "Указанный провайдер уже привязан.");

        _linkedProviders.Add(provider);
        MarkAsUpdated(now);
        return Result.Success();
    }

    /// <summary>
    /// Отвязывает внешний провайдер от аккаунта.
    /// </summary>
    /// <param name="type">Тип внешнего провайдера аутентификации.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если провайдер отвязан;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.ExternalProviderNotLinked"/>, если провайдер не найден в привязках.
    /// </returns>
    internal Result UnlinkProvider(ExternalProviderType type, DateTimeOffset now)
    {
        if (!_linkedProviders.Exists(x => x.Type == type))
            return Error.NotFound(ErrorCodes.Auth.UserAccount.ExternalProviderNotLinked, "Указанный провайдер не привязан.");

        _linkedProviders.RemoveAll(x => x.Type == type);
        MarkAsUpdated(now);
        return Result.Success();
    }
}
