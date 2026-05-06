namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Extensions;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;

/// <summary>
/// Методы расширения для перечисления <see cref="OtpType"/>.
/// </summary>
public static class OtpTypeExtensions
{
    /// <summary>
    /// Возвращает способ доставки одноразового кода, соответствующий типу OTP.
    /// </summary>
    /// <param name="type">Тип одноразового пароля.</param>
    /// <returns>
    /// <see cref="DeliveryMethod.Email"/> для всех текущих значений <see cref="OtpType"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Если передано неизвестное значение <paramref name="type"/>.
    /// </exception>
    public static DeliveryMethod GetDeliveryMethod(this OtpType type) => type switch
    {
        OtpType.EmailConfirmation or OtpType.MfaEmail or OtpType.PasswordReset => DeliveryMethod.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
