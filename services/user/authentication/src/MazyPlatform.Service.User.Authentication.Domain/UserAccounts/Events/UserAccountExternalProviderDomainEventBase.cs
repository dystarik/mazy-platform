namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Базовый класс для доменных событий, связанных с внешними провайдерами аккаунта.
/// </summary>
/// <remarks>
/// Расширяет <see cref="UserAccountDomainEventBase"/> полем <see cref="ProviderType"/>,
/// которое указывает тип затронутого внешнего провайдера.
/// </remarks>
/// <seealso cref="UserAccountExternalProviderAddedDomainEvent"/>
/// <seealso cref="UserAccountExternalProviderRemovedDomainEvent"/>
public abstract record UserAccountExternalProviderDomainEventBase : UserAccountDomainEventBase
{
    /// <summary>
    /// Инициализирует базовые поля события внешнего провайдера.
    /// </summary>
    /// <param name="occurredAt">Временная метка UTC момента возникновения события.</param>
    /// <param name="userId">Идентификатор аккаунта.</param>
    /// <param name="email">Email аккаунта.</param>
    /// <param name="providerType">Тип внешнего провайдера, затронутый операцией.</param>
    protected UserAccountExternalProviderDomainEventBase(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        ExternalProviderType providerType)
        : base(occurredAt, userId, email)
    {
        ProviderType = providerType;
    }

    /// <summary>
    /// Тип внешнего провайдера, с которым связано событие.
    /// </summary>
    public ExternalProviderType ProviderType { get; }
}
