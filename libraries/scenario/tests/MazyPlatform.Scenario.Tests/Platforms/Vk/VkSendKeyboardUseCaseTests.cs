namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Tests.Helpers;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class VkSendKeyboardUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_AlwaysSendsReplyKeyboard()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 1}""");

        var result = await useCase.ExecuteAsync(
            CreateContext(),
            "Выберите действие",
            [[new VkButtonInfo("Да", "yes")]],
            oneTime: true);

        var form = HttpUtility.ParseQueryString(capture.Body!);
        using var keyboard = ParseKeyboard(capture.Body);
        await Assert.That(form["peer_id"]).IsEqualTo("test_chat");
        await Assert.That(form["peer_ids"]).IsNull();
        await Assert.That(result.MessageId).IsEqualTo("vk:mid:1");
        await Assert.That(keyboard.RootElement.GetProperty("inline").GetBoolean()).IsFalse();
        await Assert.That(keyboard.RootElement.GetProperty("one_time").GetBoolean()).IsTrue();
        await Assert.That(keyboard.RootElement.GetProperty("buttons")[0][0].GetProperty("action").GetProperty("type").GetString())
            .IsEqualTo("callback");
        await Assert.That(keyboard.RootElement.GetProperty("buttons")[0][0].GetProperty("action").GetProperty("payload").GetString())
            .IsEqualTo("\"yes\"");
    }

    [Test]
    public async Task ExecuteAsync_WithOneTimeFalse_SendsOneTimeFalse()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 1}""");

        await useCase.ExecuteAsync(
            CreateContext(),
            "Выберите действие",
            [[new VkButtonInfo("Да", "yes")]],
            oneTime: false);

        using var keyboard = ParseKeyboard(capture.Body);
        await Assert.That(keyboard.RootElement.GetProperty("inline").GetBoolean()).IsFalse();
        await Assert.That(keyboard.RootElement.GetProperty("one_time").GetBoolean()).IsFalse();
        await Assert.That(keyboard.RootElement.GetProperty("buttons")[0][0].GetProperty("action").GetProperty("type").GetString())
            .IsEqualTo("callback");
        await Assert.That(keyboard.RootElement.GetProperty("buttons")[0][0].GetProperty("action").GetProperty("payload").GetString())
            .IsEqualTo("\"yes\"");
    }

    private static JsonDocument ParseKeyboard(string? requestBody)
    {
        var form = HttpUtility.ParseQueryString(requestBody!);
        return JsonDocument.Parse(form["keyboard"]!);
    }

    private static (VkSendKeyboardUseCase UseCase, RequestCapture Capture) CreateUseCase(string vkResponseJson)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(HttpStatusCode.OK, vkResponseJson, capture);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        var apiClient = new VkApiClient(factory, Options.Create(new VkOptions()));
        return (new VkSendKeyboardUseCase(apiClient), capture);
    }

    private static ExecutionContext CreateContext() =>
        new()
        {
            Session = new TestSession(),
            IncomingEvent = new TestIncomingEvent { ChatId = "test_chat" },
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = "test_token",
            ProjectId = Guid.NewGuid(),
        };

    private sealed class RequestCapture
    {
        public string? Body { get; set; }
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string responseBody, RequestCapture capture) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture.Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody),
            };
        }
    }
}
