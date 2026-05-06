namespace MazyPlatform.Scenario.Tests.Platforms.Telegram;

using System.Net;
using System.Text;

using MazyPlatform.Scenario.Telegram.Api;
using MazyPlatform.Scenario.Telegram.Configuration;

using Microsoft.Extensions.Options;

public class TelegramApiClientTests
{
    [Test]
    public async Task CallAsync_OkTrue_ReturnsResult()
    {
        using var handler = new DelegateHandler(static _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true,"result":{"message_id":42}}""", Encoding.UTF8, "application/json"),
            });
        var client = CreateClient(handler);

        var result = await client.CallAsync("sendMessage", new { chat_id = "1", text = "Привет" }, "TOKEN");

        await Assert.That(result.GetProperty("message_id").GetInt32()).IsEqualTo(42);
    }

    [Test]
    public async Task CallAsync_OkFalse_ThrowsTelegramApiException()
    {
        using var handler = new DelegateHandler(static _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":false,"error_code":400,"description":"Bad Request"}""", Encoding.UTF8, "application/json"),
            });
        var client = CreateClient(handler);
        TelegramApiException? exception = null;

        try
        {
            await client.CallAsync("sendMessage", new { chat_id = "1", text = "Привет" }, "TOKEN");
        }
        catch (TelegramApiException ex)
        {
            exception = ex;
        }

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.ErrorCode).IsEqualTo(400);
        await Assert.That(exception.Description).IsEqualTo("Bad Request");
    }

    private static TelegramApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new FakeHttpClientFactory(handler),
            Options.Create(new TelegramOptions { BaseUrl = "https://telegram.test" }));

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handle(request));
    }
}
