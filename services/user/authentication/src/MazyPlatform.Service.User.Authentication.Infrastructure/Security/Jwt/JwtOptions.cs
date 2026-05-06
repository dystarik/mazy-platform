namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security.Jwt;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Параметры конфигурации JWT, считываемые из секции <see cref="SectionName"/> файла настроек.
/// </summary>
internal sealed class JwtOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Секретный ключ для подписи токена (HMAC-SHA256). Минимальная длина — 32 символа.
    /// </summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    [MinLength(32, ErrorMessage = "Длина {0} должна быть не менее {1} символов.")]
    public required string SecretKey { get; init; }

    /// <summary>Издатель токена (<c>iss</c>).</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string Issuer { get; init; }

    /// <summary>Аудитория токена (<c>aud</c>).</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string Audience { get; init; }

    /// <summary>
    /// Срок жизни токена в минутах. Допустимый диапазон: от 1 до 60.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Значение {0} должно находиться в диапазоне от {1} до {2}.")]
    public required int ExpirationMinutes { get; init; }
}
