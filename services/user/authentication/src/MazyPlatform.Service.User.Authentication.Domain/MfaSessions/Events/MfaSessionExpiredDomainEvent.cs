namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions.Events;

/// <summary>
/// Доменное событие, поднимаемое при обнаружении истёкшей MFA-сессии.
/// </summary>
/// <remarks>
/// Публикуется в методе <c>BasicValidate</c> внутри <see cref="MfaSession"/>,
/// когда <see cref="MfaSession.ExpiresAt"/> меньше текущего времени на момент
/// попытки добавления фактора. Служит для аудита и мониторинга устаревших сессий.
/// </remarks>
/// <seealso cref="MfaSessionDomainEventBase"/>
public sealed record class MfaSessionExpiredDomainEvent : MfaSessionDomainEventBase
{
    public MfaSessionExpiredDomainEvent(DateTimeOffset occurredAt, Guid userAccountId, MfaSessionAction action)
        : base(occurredAt, userAccountId, action) { }
}
