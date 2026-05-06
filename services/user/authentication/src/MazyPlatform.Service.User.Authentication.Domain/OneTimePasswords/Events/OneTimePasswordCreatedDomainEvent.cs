namespace MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Events;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Доменное событие, поднимаемое при создании нового одноразового пароля.
/// </summary>
/// <remarks>
/// Публикуется из <see cref="OneTimePassword.Create"/>.
/// Содержит открытый код (<see cref="Code"/>), который должен быть доставлен пользователю
/// обработчиком события (через email или SMS). Код нигде больше не хранится в открытом виде.
/// </remarks>
/// <seealso cref="DomainEventBase"/>
public sealed record OneTimePasswordCreatedDomainEvent : DomainEventBase
{
    internal OneTimePasswordCreatedDomainEvent(
        DateTimeOffset occurredAt,
        Guid oneTimePasswordId,
        Guid userAccountId,
        string code,
        OtpType type,
        DateTimeOffset expiresAt)
     : base(occurredAt)
    {
        OneTimePasswordId = oneTimePasswordId;
        UserAccountId = userAccountId;
        Code = code;
        Type = type;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Идентификатор созданного одноразового пароля.
    /// </summary>
    public Guid OneTimePasswordId { get; init; }

    /// <summary>
    /// Идентификатор аккаунта, для которого создан OTP.
    /// </summary>
    public Guid UserAccountId { get; init; }

    /// <summary>
    /// Открытый числовой код, который необходимо доставить пользователю.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Тип одноразового пароля, определяющий канал и контекст доставки.
    /// </summary>
    public OtpType Type { get; init; }

    /// <summary>
    /// Временная метка UTC истечения срока действия кода.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
