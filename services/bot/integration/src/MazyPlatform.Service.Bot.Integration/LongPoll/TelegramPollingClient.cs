namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

internal sealed class TelegramPollingClient(IHttpClientFactory httpClientFactory)
{
    public async Task<TelegramUpdatesResponse> GetUpdatesAsync(
        string accessToken,
        long? offset,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("TelegramApi");

        var url = $"/bot{accessToken}/getUpdates?timeout={timeoutSeconds.ToString(CultureInfo.InvariantCulture)}";
        if (offset is not null)
            url += $"&offset={offset.Value.ToString(CultureInfo.InvariantCulture)}";

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken);

        if (payload is null)
            throw new InvalidOperationException("Telegram getUpdates returned an empty response.");

        var root = payload.RootElement;
        if (!root.TryGetProperty("ok", out var okProperty) || !okProperty.GetBoolean())
        {
            var description = root.TryGetProperty("description", out var descriptionProperty)
                ? descriptionProperty.GetString()
                : "unknown error";

            throw new InvalidOperationException($"Telegram getUpdates failed: {description}");
        }

        if (!root.TryGetProperty("result", out var resultProperty) || resultProperty.ValueKind != JsonValueKind.Array)
            return new TelegramUpdatesResponse([]);

        var updates = new List<JsonElement>();
        foreach (var update in resultProperty.EnumerateArray())
        {
            updates.Add(update.Clone());
        }

        return new TelegramUpdatesResponse(updates);
    }
}
