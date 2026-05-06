namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions.Events;

/// <summary>
/// Доменное событие, поднимаемое при создании новой MFA-сессии.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="MfaSession.Create"/>.
/// Обработчики могут использовать событие для аудита попыток прохождения MFA.
/// </remarks>
/// <seealso cref="MfaSessionDomainEventBase"/>
public sealed record MfaSessionCreatedDomainEvent : MfaSessionDomainEventBase
{
    public MfaSessionCreatedDomainEvent(DateTimeOffset occurredAt, Guid userAccountId, MfaSessionAction action)
        : base(occurredAt, userAccountId, action) { }
}
