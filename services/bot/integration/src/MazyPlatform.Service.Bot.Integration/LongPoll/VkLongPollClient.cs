namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using System.Globalization;
using System.Text.Json;

/// <summary>
/// HTTP-клиент для работы с VK Long Poll API.
/// </summary>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов.</param>
internal sealed class VkLongPollClient(IHttpClientFactory httpClientFactory)
{
    private const string BaseApiUrl = "https://api.vk.com/method/";

    /// <summary>
    /// Получает данные Long Poll сервера для сообщества.
    /// </summary>
    /// <param name="groupId">Идентификатор сообщества VK.</param>
    /// <param name="token">Токен доступа.</param>
    /// <param name="apiVersion">Версия VK API.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные Long Poll сервера.</returns>
    public async Task<LongPollServer> GetServerAsync(
        int groupId,
        string token,
        string apiVersion,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient("VkApi");

        var url = $"{BaseApiUrl}groups.getLongPollServer" +
                  $"?group_id={groupId.ToString(CultureInfo.InvariantCulture)}" +
                  $"&access_token={token}" +
                  $"&v={apiVersion}";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error))
        {
            var code = error.GetProperty("error_code").GetInt32();
            var msg = error.GetProperty("error_msg").GetString() ?? "Unknown error";
            throw new InvalidOperationException($"VK API error {code.ToString(CultureInfo.InvariantCulture)}: {msg}");
        }

        var result = root.GetProperty("response");

        return new LongPollServer(
            Server: result.GetProperty("server").GetString()!,
            Key: result.GetProperty("key").GetString()!,
            Ts: result.GetProperty("ts").GetString()!);
    }

    /// <summary>
    /// Выполняет один Long Poll запрос.
    /// </summary>
    /// <param name="server">Данные Long Poll сервера.</param>
    /// <param name="waitSeconds">Время ожидания в секундах.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Ответ с событиями.</returns>
    public async Task<LongPollResponse> PollAsync(
        LongPollServer server,
        int waitSeconds,
        CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient("VkLongPoll");

        var url = $"{server.Server}" +
                  $"?act=a_check" +
                  $"&key={server.Key}" +
                  $"&ts={server.Ts}" +
                  $"&wait={waitSeconds.ToString(CultureInfo.InvariantCulture)}";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (root.TryGetProperty("failed", out var failedProp))
        {
            var ts = root.TryGetProperty("ts", out var tsProp) ? tsProp.GetString() : null;
            return new LongPollResponse(ts, [], failedProp.GetInt32());
        }

        var newTs = root.GetProperty("ts").GetString()!;
        var updates = new List<JsonElement>();

        foreach (var update in root.GetProperty("updates").EnumerateArray())
            updates.Add(update.Clone());

        return new LongPollResponse(newTs, updates, null);
    }
}
