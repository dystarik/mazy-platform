namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions.Events;

/// <summary>
/// Доменное событие, поднимаемое при успешном завершении MFA-сессии.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="MfaSession.CompleteWithFactor"/> или <see cref="MfaSession.CompleteWithBackupCode"/>
/// в момент, когда набрано необходимое количество факторов.
/// После завершения сессия действует в течение 30 минут (<see cref="MfaSession.ValidUntil"/>).
/// </remarks>
/// <seealso cref="MfaSessionDomainEventBase"/>
public sealed record MfaSessionCompletedDomainEvent : MfaSessionDomainEventBase
{
    public MfaSessionCompletedDomainEvent(DateTimeOffset occurredAt, Guid userAccountId, MfaSessionAction action, bool isCompletedByBackupCode = false)
        : base(occurredAt, userAccountId, action)
    {
        IsCompletedByBackupCode = isCompletedByBackupCode;
    }

    /// <summary>
    /// Возвращает <see langword="true"/>, если сессия завершена через резервный код, а не через основной фактор MFA.
    /// </summary>
    public bool IsCompletedByBackupCode { get; }
}
