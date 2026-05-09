namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
using System.Net.Http;
using System.Web;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Tests.Helpers;
using MazyPlatform.Scenario.Vk.Actions;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class VkRemoveKeyboardUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_ApiReturnsNumber_ReturnsMessageId()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 12345}""");

        var result = await useCase.ExecuteAsync(CreateContext(), "Готово");

        var form = HttpUtility.ParseQueryString(capture.Body!);
        await Assert.That(form["peer_id"]).IsEqualTo("test_chat");
        await Assert.That(form["peer_ids"]).IsNull();
        await Assert.That(result.MessageId).IsEqualTo("vk:mid:12345");
        await Assert.That(result.Action).IsTypeOf<VkRemoveKeyboardAction>();
    }

    [Test]
    public async Task ExecuteAsync_ApiReturnsObject_ReturnsNullMessageId()
    {
        var (useCase, _) = CreateUseCase("""{"response": {"id": 1}}""");

        var result = await useCase.ExecuteAsync(CreateContext(), "Готово");

        await Assert.That(result.MessageId).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_BuildsEmptyKeyboardJson()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 1}""");

        await useCase.ExecuteAsync(CreateContext(), "Готово");

        var form = HttpUtility.ParseQueryString(capture.Body!);
        await Assert.That(form["keyboard"]).IsEqualTo("{\"buttons\":[]}");
        await Assert.That(form["message"]).IsEqualTo("Готово");
    }

    private static (VkRemoveKeyboardUseCase UseCase, RequestCapture Capture) CreateUseCase(string vkResponseJson)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(HttpStatusCode.OK, vkResponseJson, capture);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        var apiClient = new VkApiClient(factory, Options.Create(new VkOptions()));
        return (new VkRemoveKeyboardUseCase(apiClient), capture);
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
