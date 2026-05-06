namespace MazyPlatform.SharedKernel.Api.Extensions;

using System.Globalization;
using System.Net;

using Grpc.Core;

/// <summary>
/// Методы расширения для <see cref="ServerCallContext"/>,
/// предоставляющие доступ к стандартным заголовкам платформы MazyPlatform.
/// </summary>
/// <remarks>
/// Заголовки устанавливаются API Gateway после проверки JWT-токена
/// и передаются во все внутренние gRPC-сервисы.
/// </remarks>
public static class ServerCallContextExtensions
{
    private const string _userIdHeaderName = "x-user-id";
    private const string _tokenIdHeaderName = "x-token-id";
    private const string _forwardedForHeaderName = "x-forwarded-for";

    /// <summary>
    /// Извлекает идентификатор аккаунта пользователя из заголовка <c>x-user-id</c>.
    /// </summary>
    /// <param name="context">Контекст gRPC-вызова.</param>
    /// <returns>Строковый идентификатор пользователя.</returns>
    /// <exception cref="RpcException">
    /// Выбрасывается со статусом <see cref="StatusCode.Unauthenticated"/>,
    /// если заголовок отсутствует или пуст.
    /// </exception>
    public static string GetUserAccountId(this ServerCallContext context)
    {
        return GetRequiredHeaderValue(context.RequestHeaders, _userIdHeaderName, "Идентификатор пользователя отсутствует.");
    }

    /// <summary>
    /// Извлекает идентификатор refresh-токена из заголовка <c>x-token-id</c>.
    /// </summary>
    /// <param name="context">Контекст gRPC-вызова.</param>
    /// <returns>Строковый идентификатор refresh-токена.</returns>
    /// <exception cref="RpcException">
    /// Выбрасывается со статусом <see cref="StatusCode.Unauthenticated"/>,
    /// если заголовок отсутствует или пуст.
    /// </exception>
    public static string GetRefreshTokenId(this ServerCallContext context)
    {
        return GetRequiredHeaderValue(context.RequestHeaders, _tokenIdHeaderName, "Идентификатор токена отсутствует.");
    }

    /// <summary>
    /// Определяет IP-адрес клиента из заголовка <c>x-forwarded-for</c>
    /// или из свойства <see cref="ServerCallContext.Peer"/>.
    /// </summary>
    /// <param name="context">Контекст gRPC-вызова.</param>
    /// <returns>Нормализованная строка IP-адреса клиента.</returns>
    /// <exception cref="RpcException">
    /// Выбрасывается со статусом <see cref="StatusCode.Internal"/>,
    /// если IP-адрес не удалось определить ни из одного источника.
    /// </exception>
    public static string GetClientIpAddress(this ServerCallContext context)
    {
        var forwardedFor = GetHeaderValue(context.RequestHeaders, _forwardedForHeaderName);

        if (TryParseForwardedForIpAddress(forwardedFor, out var forwardedForIpAddress))
            return forwardedForIpAddress;

        if (TryParsePeerIpAddress(context.Peer, out var peerIpAddress))
            return peerIpAddress;

        throw new RpcException(new Status(StatusCode.Internal, "Не удалось определить IP адрес клиента."));
    }

    /// <summary>
    /// Возвращает значение обязательного заголовка или выбрасывает <see cref="RpcException"/>.
    /// </summary>
    private static string GetRequiredHeaderValue(Metadata headers, string headerName, string errorMessage)
    {
        var value = GetHeaderValue(headers, headerName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new RpcException(new Status(StatusCode.Unauthenticated, errorMessage))
            : value;
    }

    /// <summary>
    /// Возвращает значение заголовка или <see langword="null"/>, если заголовок отсутствует.
    /// </summary>
    private static string? GetHeaderValue(Metadata headers, string headerName)
    {
        return headers.FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    /// <summary>
    /// Пытается извлечь IP-адрес из заголовка <c>x-forwarded-for</c>.
    /// Берёт первый адрес из списка (ближайший клиент).
    /// </summary>
    private static bool TryParseForwardedForIpAddress(string? forwardedFor, out string ipAddress)
    {
        ipAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(forwardedFor))
            return false;

        var firstForwarded = forwardedFor.Split(',', 2)[0].Trim();
        return TryNormalizeIpAddress(firstForwarded, out ipAddress);
    }

    /// <summary>
    /// Пытается извлечь IP-адрес из свойства <see cref="ServerCallContext.Peer"/>.
    /// </summary>
    private static bool TryParsePeerIpAddress(string? peer, out string ipAddress)
    {
        ipAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(peer))
            return false;

        if (Uri.TryCreate(peer, UriKind.Absolute, out var uri))
            return TryNormalizeIpAddress(uri.Host, out ipAddress);

        var colonIndex = peer.IndexOf(':', StringComparison.OrdinalIgnoreCase);
        if (colonIndex < 0 || colonIndex + 1 >= peer.Length)
            return false;

        var addressPart = peer[(colonIndex + 1)..];
        return TryNormalizeIpAddress(addressPart, out ipAddress);
    }

    /// <summary>
    /// Нормализует строку IP-адреса: убирает порт, квадратные скобки IPv6.
    /// </summary>
    /// <param name="value">Исходная строка.</param>
    /// <param name="ipAddress">Нормализованный IP-адрес при успехе.</param>
    /// <returns><see langword="true"/>, если адрес успешно распознан.</returns>
    private static bool TryNormalizeIpAddress(string? value, out string ipAddress)
    {
        ipAddress = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var candidate = value.Trim();

        if (IPAddress.TryParse(candidate, out var parsed))
        {
            ipAddress = parsed.ToString();
            return true;
        }

        if (candidate.StartsWith('['))
        {
            var endBracketIndex = candidate.IndexOf(']', StringComparison.OrdinalIgnoreCase);
            if (endBracketIndex > 1)
                return TryNormalizeIpAddress(candidate[1..endBracketIndex], out ipAddress);
        }

        var lastColonIndex = candidate.LastIndexOf(':');
        if (lastColonIndex > 0 && lastColonIndex + 1 < candidate.Length)
        {
            var hostPart = candidate[..lastColonIndex];
            var portPart = candidate[(lastColonIndex + 1)..];

            if (int.TryParse(portPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return TryNormalizeIpAddress(hostPart, out ipAddress);
        }

        return false;
    }
}
