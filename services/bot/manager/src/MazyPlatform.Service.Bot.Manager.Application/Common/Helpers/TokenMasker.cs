namespace MazyPlatform.Service.Bot.Manager.Application.Common.Helpers;

internal static class TokenMasker
{
    private const int VisiblePrefixLength = 6;
    private const int VisibleSuffixLength = 4;
    private const string MaskString = "****";
    private const string FullMaskString = "********";

    /// <summary>
    /// Маскирует токен, оставляя видимыми первые 6 и последние 4 символа.
    /// Если токен короче 10 символов — возвращает полностью замаскированную строку.
    /// </summary>
    /// <param name="token">Токен для маскировки.</param>
    /// <returns>Маскированный токен.</returns>
    public static string Mask(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (token.Length < VisiblePrefixLength + VisibleSuffixLength)
            return FullMaskString;

        return string.Concat(
            token.AsSpan(0, VisiblePrefixLength),
            MaskString,
            token.AsSpan(token.Length - VisibleSuffixLength));
    }
}
