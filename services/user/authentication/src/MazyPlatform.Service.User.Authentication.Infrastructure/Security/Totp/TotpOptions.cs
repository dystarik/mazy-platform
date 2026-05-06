namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Параметры конфигурации TOTP-верификации, считываемые из секции <see cref="SectionName"/> файла настроек.
/// </summary>
internal sealed class TotpOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Totp";

    /// <summary>
    /// Название издателя, отображаемое в приложении-аутентификаторе. Минимальная длина — 4 символа.
    /// </summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    [MinLength(4, ErrorMessage = "Длина {0} должна быть не менее {1} символов.")]
    public required string Issuer { get; init; }

    /// <summary>
    /// Количество прошедших временных шагов (30 с каждый), принимаемых при верификации.
    /// Позволяет компенсировать запаздывание часов клиента. Допустимый диапазон: от 0 до 2.
    /// </summary>
    [Range(0, 2, ErrorMessage = "Значение {0} должно находиться в диапазоне от {1} до {2}.")]
    public required int VerificationWindowPastSteps { get; init; }

    /// <summary>
    /// Количество будущих временных шагов (30 с каждый), принимаемых при верификации.
    /// Позволяет компенсировать опережение часов клиента. Допустимый диапазон: от 0 до 2.
    /// </summary>
    [Range(0, 2, ErrorMessage = "Значение {0} должно находиться в диапазоне от {1} до {2}.")]
    public required int VerificationWindowFutureSteps { get; init; }
}
