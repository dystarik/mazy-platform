namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Security;

using System.ComponentModel.DataAnnotations;

internal sealed class BotTokenEncryptionOptions
{
    public const string SectionName = "BotTokenEncryption";

    /// <summary>
    /// Ключ шифрования AES-256 в формате Base64. Минимальная длина строки — 44 символа (32 байта).
    /// </summary>
    [Required]
    [MinLength(44, ErrorMessage = "{0} должен быть Base64-строкой длиной не менее 44 символов (32 байта AES-256).")]
    public required string Key { get; init; }
}
