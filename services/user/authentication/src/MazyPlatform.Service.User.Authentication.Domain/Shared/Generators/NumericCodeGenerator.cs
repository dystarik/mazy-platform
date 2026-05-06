namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Generators;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Генерирует криптографически стойкие числовые коды произвольной длины.
/// </summary>
/// <remarks>
/// Используется в <see cref="OneTimePassword.Create"/> для формирования OTP-кодов.
/// Каждая цифра выбирается через <see cref="RandomNumberGenerator.GetInt32(int, int)"/>,
/// что исключает статистические смещения.
/// </remarks>
public static class NumericCodeGenerator
{
    /// <summary>
    /// Генерирует строку из случайных десятичных цифр указанной длины.
    /// </summary>
    /// <param name="length">Требуемая длина кода. Должна быть больше нуля.</param>
    /// <returns>Строка цифр длиной <paramref name="length"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Если <paramref name="length"/> меньше 1.
    /// </exception>
    public static string Generate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);

        var code = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            code.Append(RandomNumberGenerator.GetInt32(0, 10));
        }

        return code.ToString();
    }
}
