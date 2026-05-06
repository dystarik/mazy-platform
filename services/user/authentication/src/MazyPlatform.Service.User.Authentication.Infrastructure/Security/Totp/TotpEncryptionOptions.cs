namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Параметры конфигурации шифрования TOTP-секретов, считываемые из секции <see cref="SectionName"/> файла настроек.
/// </summary>
internal sealed class TotpEncryptionOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "TotpEncryption";

    /// <summary>
    /// Ключ шифрования AES-256 в формате Base64. Минимальная длина строки — 44 символа (32 байта).
    /// </summary>
    [Required]
    [MinLength(44, ErrorMessage = "{0} должен быть Base64-строкой длиной не менее 44 символов (32 байта AES-256).")]
    public required string Key { get; init; }
}
