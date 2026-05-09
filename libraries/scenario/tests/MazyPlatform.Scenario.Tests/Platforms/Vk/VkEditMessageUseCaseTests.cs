namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
using System.Text.Json;
using System.Web;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Tests.Helpers;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class VkEditMessageUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_WithButtons_BuildsInlineKeyboardRowsAndMapsStyles()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 1}""");
        var layout = new ButtonLayout(
        [
            [new ButtonDefinition("Ок", "ok", ButtonStyle.Success)],
            [new ButtonDefinition("Нет", "no", ButtonStyle.Danger)],
        ]);

        await useCase.ExecuteAsync(CreateContext(), "123", "Обновлено", layout);

        using var keyboard = ParseKeyboard(capture.Body);
        var root = keyboard.RootElement;
        var rows = root.GetProperty("buttons");

        await Assert.That(root.GetProperty("inline").GetBoolean()).IsTrue();
        await Assert.That(rows.GetArrayLength()).IsEqualTo(2);
        await Assert.That(rows[0][0].GetProperty("action").GetProperty("type").GetString()).IsEqualTo("callback");
        await Assert.That(rows[0][0].GetProperty("action").GetProperty("payload").GetString()).IsEqualTo("ok");
        await Assert.That(rows[0][0].GetProperty("color").GetString()).IsEqualTo("positive");
        await Assert.That(rows[1][0].GetProperty("action").GetProperty("type").GetString()).IsEqualTo("callback");
        await Assert.That(rows[1][0].GetProperty("action").GetProperty("payload").GetString()).IsEqualTo("no");
        await Assert.That(rows[1][0].GetProperty("color").GetString()).IsEqualTo("negative");
    }

    [Test]
    public async Task ExecuteAsync_WithNullButtons_DoesNotSendKeyboard()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 1}""");

        await useCase.ExecuteAsync(CreateContext(), "123", "Обновлено", buttons: null);

        var form = HttpUtility.ParseQueryString(capture.Body!);
        await Assert.That(form["keyboard"]).IsNull();
    }

    private static JsonDocument ParseKeyboard(string? requestBody)
    {
        var form = HttpUtility.ParseQueryString(requestBody!);
        return JsonDocument.Parse(form["keyboard"]!);
    }

    private static (VkEditMessageUseCase UseCase, RequestCapture Capture) CreateUseCase(string vkResponseJson)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(HttpStatusCode.OK, vkResponseJson, capture);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        var apiClient = new VkApiClient(factory, Options.Create(new VkOptions()));
        return (new VkEditMessageUseCase(apiClient), capture);
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
