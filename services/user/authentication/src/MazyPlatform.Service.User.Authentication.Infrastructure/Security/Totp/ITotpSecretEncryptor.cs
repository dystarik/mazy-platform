namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;

/// <summary>
/// Контракт для симметричного шифрования и расшифрования TOTP-секретов перед сохранением в хранилище.
/// </summary>
/// <seealso cref="AesGcmService"/>
public interface ITotpSecretEncryptor
{
    /// <summary>
    /// Шифрует открытый текст и возвращает зашифрованное значение в формате Base64.
    /// </summary>
    /// <param name="plaintext">Открытый текст для шифрования.</param>
    /// <returns>Зашифрованное значение в формате Base64.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Расшифровывает значение в формате Base64 и возвращает открытый текст.
    /// </summary>
    /// <param name="ciphertext">Зашифрованное значение в формате Base64.</param>
    /// <returns>Расшифрованный открытый текст.</returns>
    string Decrypt(string ciphertext);
}
