namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;

/// <summary>
/// Маркерный интерфейс полезной нагрузки метода многофакторной аутентификации.
/// </summary>
/// <remarks>
/// Реализуется конкретными классами: <see cref="TotpPayload"/> и <see cref="EmailPayload"/>.
/// Хранится в <see cref="MfaMethod.Payload"/> и используется для полиморфной обработки
/// методов MFA в <see cref="VerifyMfaFactorService"/>.
/// </remarks>
/// <seealso cref="TotpPayload"/>
/// <seealso cref="EmailPayload"/>
public interface IMfaPayload
{
    /// <summary>
    /// Тип MFA-метода, соответствующий данной полезной нагрузке.
    /// </summary>
    public MfaMethodType Type { get; }
}
