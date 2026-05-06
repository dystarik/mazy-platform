namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Базовый класс для доменных событий, связанных с методами MFA аккаунта.
/// </summary>
/// <remarks>
/// Расширяет <see cref="UserAccountDomainEventBase"/> полем <see cref="MethodType"/>,
/// которое несёт тип MFA-метода, затронутого операцией.
/// </remarks>
/// <seealso cref="UserAccountMfaMethodAddedDomainEvent"/>
/// <seealso cref="UserAccountMfaMethodConfirmedDomainEvent"/>
/// <seealso cref="UserAccountMfaMethodRemovedDomainEvent"/>
public abstract record UserAccountMfaMethodDomainEventBase : UserAccountDomainEventBase
{
    /// <summary>
    /// Инициализирует базовые поля события MFA-метода.
    /// </summary>
    /// <param name="occurredAt">Временная метка UTC момента возникновения события.</param>
    /// <param name="userId">Идентификатор аккаунта.</param>
    /// <param name="email">Email аккаунта.</param>
    /// <param name="methodType">Тип MFA-метода, затронутого операцией.</param>
    protected UserAccountMfaMethodDomainEventBase(
        DateTimeOffset occurredAt,
        Guid userId,
        Email email,
        MfaMethodType methodType)
    : base(occurredAt, userId, email) => MethodType = methodType;

    /// <summary>
    /// Тип MFA-метода, с которым связано событие.
    /// </summary>
    public MfaMethodType MethodType { get; }
}
