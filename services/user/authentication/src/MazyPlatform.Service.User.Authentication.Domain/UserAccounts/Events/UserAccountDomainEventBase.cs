namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Базовый класс для всех доменных событий агрегата <see cref="UserAccount"/>.
/// </summary>
/// <remarks>
/// Несёт идентификатор аккаунта и email, которые нужны всем обработчикам событий аккаунта.
/// </remarks>
/// <seealso cref="DomainEventBase"/>
public abstract record class UserAccountDomainEventBase : DomainEventBase
{
    /// <summary>
    /// Инициализирует базовые поля события аккаунта.
    /// </summary>
    /// <param name="occurredAt">Временная метка UTC момента возникновения события.</param>
    /// <param name="userId">Идентификатор аккаунта, в котором произошло событие.</param>
    /// <param name="email">Email аккаунта на момент возникновения события.</param>
    protected UserAccountDomainEventBase(DateTimeOffset occurredAt, Guid userId, Email email)
        : base(occurredAt)
    {
        UserAccountId = userId;
        Email = email;
    }

    /// <summary>
    /// Идентификатор аккаунта, в котором произошло событие.
    /// </summary>
    public Guid UserAccountId { get; }

    /// <summary>
    /// Email аккаунта на момент возникновения события.
    /// </summary>
    public Email Email { get; }
}
