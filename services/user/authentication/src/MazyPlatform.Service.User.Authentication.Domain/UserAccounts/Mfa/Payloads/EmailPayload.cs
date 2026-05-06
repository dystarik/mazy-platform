namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Полезная нагрузка метода MFA с доставкой кода по email.
/// </summary>
/// <remarks>
/// Хранит адрес, на который будет отправлен OTP-код при прохождении MFA.
/// Создаётся при вызове <see cref="UserAccount.AddMfaMethod"/> с типом <see cref="MfaMethodType.Email"/>.
/// </remarks>
/// <seealso cref="MfaSettings.EmailMethod"/>
public sealed record EmailPayload : IMfaPayload
{
    /// <summary>
    /// Инициализирует полезную нагрузку Email-метода MFA.
    /// </summary>
    /// <param name="email">Email-адрес для доставки кода подтверждения.</param>
    /// <exception cref="ArgumentNullException">
    /// Если <paramref name="email"/> равен <see langword="null"/>.
    /// </exception>
    public EmailPayload(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        Email = email;
    }

    /// <inheritdoc />
    public MfaMethodType Type => MfaMethodType.Email;

    /// <summary>
    /// Email-адрес для доставки кода подтверждения MFA.
    /// </summary>
    public Email Email { get; }
}
