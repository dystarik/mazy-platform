namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions.Events;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Базовый класс для всех доменных событий агрегата <see cref="MfaSession"/>.
/// </summary>
/// <remarks>
/// Несёт идентификатор аккаунта и целевое действие, которые необходимы всем обработчикам событий MFA-сессии.
/// </remarks>
/// <seealso cref="DomainEventBase"/>
public abstract record MfaSessionDomainEventBase : DomainEventBase
{
    /// <summary>
    /// Инициализирует базовые поля события MFA-сессии.
    /// </summary>
    /// <param name="occurredAt">Временная метка UTC момента возникновения события.</param>
    /// <param name="userAccountId">Идентификатор аккаунта, проходящего MFA.</param>
    /// <param name="action">Целевое действие, для которого создана сессия.</param>
    protected MfaSessionDomainEventBase(DateTimeOffset occurredAt, Guid userAccountId, MfaSessionAction action)
        : base(occurredAt)
    {
        UserAccountId = userAccountId;
        Action = action;
    }

    /// <summary>
    /// Идентификатор аккаунта, проходящего MFA.
    /// </summary>
    public Guid UserAccountId { get; }

    /// <summary>
    /// Целевое действие, которое защищает данная MFA-сессия.
    /// </summary>
    public MfaSessionAction Action { get; }
}
