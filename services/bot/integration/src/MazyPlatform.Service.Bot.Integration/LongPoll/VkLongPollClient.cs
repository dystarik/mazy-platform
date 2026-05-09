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
    /// Включает обязательные события VK Long Poll для работы сценариев.
    /// </summary>
    /// <param name="groupId">Идентификатор сообщества VK.</param>
    /// <param name="token">Токен доступа.</param>
    /// <param name="apiVersion">Версия VK API.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task EnsureLongPollSettingsAsync(
        int groupId,
        string token,
        string apiVersion,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["group_id"] = groupId.ToString(CultureInfo.InvariantCulture),
            ["enabled"] = "1",
            ["message_new"] = "1",
            ["message_event"] = "1",
            ["access_token"] = token,
            ["v"] = apiVersion,
        };

        using var client = httpClientFactory.CreateClient("VkApi");
        using var content = new FormUrlEncodedContent(parameters);
        using var response = await client.PostAsync(
            $"{BaseApiUrl}groups.setLongPollSettings",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await EnsureVkApiSuccessAsync(response, cancellationToken);
    }

    /// <summary>
    /// Подтверждает обработку VK callback-кнопки, чтобы клиент убрал индикатор загрузки.
    /// </summary>
    /// <param name="eventId">Идентификатор события нажатия.</param>
    /// <param name="userId">Идентификатор пользователя, который нажал кнопку.</param>
    /// <param name="peerId">Идентификатор диалога.</param>
    /// <param name="token">Токен доступа сообщества.</param>
    /// <param name="apiVersion">Версия VK API.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task SendMessageEventAnswerAsync(
        string eventId,
        long userId,
        long peerId,
        string token,
        string apiVersion,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["event_id"] = eventId,
            ["user_id"] = userId.ToString(CultureInfo.InvariantCulture),
            ["peer_id"] = peerId.ToString(CultureInfo.InvariantCulture),
            ["event_data"] = string.Empty,
            ["access_token"] = token,
            ["v"] = apiVersion,
        };

        using var client = httpClientFactory.CreateClient("VkApi");
        using var content = new FormUrlEncodedContent(parameters);
        using var response = await client.PostAsync(
            $"{BaseApiUrl}messages.sendMessageEventAnswer",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await EnsureVkApiSuccessAsync(response, cancellationToken);
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

    private static async Task EnsureVkApiSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.TryGetProperty("error", out var error))
        {
            var code = error.GetProperty("error_code").GetInt32();
            var msg = error.GetProperty("error_msg").GetString() ?? "Unknown error";
            throw new InvalidOperationException($"VK API error {code.ToString(CultureInfo.InvariantCulture)}: {msg}");
        }
    }
}
