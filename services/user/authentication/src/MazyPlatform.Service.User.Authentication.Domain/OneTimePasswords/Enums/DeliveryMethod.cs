namespace MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;

/// <summary>
/// Способ доставки одноразового кода пользователю.
/// </summary>
/// <remarks>
/// Определяется автоматически из <see cref="OtpType"/> через
/// <see cref="OtpTypeExtensions.GetDeliveryMethod"/>.
/// </remarks>
public enum DeliveryMethod
{
    /// <summary>
    /// Доставка кода по электронной почте.
    /// </summary>
    Email,

    /// <summary>
    /// Доставка кода по SMS (зарезервировано для будущего использования).
    /// </summary>
    Sms,
}
