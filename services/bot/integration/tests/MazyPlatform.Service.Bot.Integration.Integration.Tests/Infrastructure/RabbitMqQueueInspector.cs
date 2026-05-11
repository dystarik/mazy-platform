namespace MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class RabbitMqQueueInspector : IDisposable
{
    private readonly HttpClient _client;

    public RabbitMqQueueInspector(string host, int managementPort, string username, string password)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri($"http://{host}:{managementPort}"),
            Timeout = TimeSpan.FromSeconds(10),
        };
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
    }

    public async Task<QueueStats> GetStatsAsync(string queueName)
    {
        using var response = await _client.GetAsync($"/api/queues/%2F/{Uri.EscapeDataString(queueName)}");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        return new QueueStats(
            Ready: GetInt32OrZero(root, "messages_ready"),
            Unacknowledged: GetInt32OrZero(root, "messages_unacknowledged"),
            Total: GetInt32OrZero(root, "messages"));
    }

    public async Task WaitForQueueExistsAsync(string queueName)
    {
        await WaitUntilAsync(
            async () =>
            {
                try
                {
                    _ = await GetStatsAsync(queueName);
                    return true;
                }
                catch (HttpRequestException)
                {
                    return false;
                }
            },
            $"Queue '{queueName}' was not created.");
    }

    public async Task WaitForQueueDrainedAsync(string queueName)
    {
        await WaitUntilAsync(
            async () =>
            {
                var stats = await GetStatsAsync(queueName);
                return stats.Total == 0;
            },
            $"Queue '{queueName}' was not drained.");
    }

    public async Task WaitForReadyMessagesAtLeastAsync(string queueName, int count)
    {
        await WaitUntilAsync(
            async () =>
            {
                var stats = await GetStatsAsync(queueName);
                return stats.Ready >= count;
            },
            $"Queue '{queueName}' did not contain at least {count} ready messages.");
    }

    public async Task AssertNoNewReadyMessagesAsync(string queueName, int previousReadyCount)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(750));
        var stats = await GetStatsAsync(queueName);
        await Assert.That(stats.Ready).IsEqualTo(previousReadyCount);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, string failureMessage)
    {
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (await condition())
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException(failureMessage);
    }

    private static int GetInt32OrZero(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.Number
            ? value.GetInt32()
            : 0;
    }
}
