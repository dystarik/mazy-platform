namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
using System.Net.Http;
using System.Web;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Tests.Helpers;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.Nodes;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class VkRemoveKeyboardNodeTests
{
    [Test]
    public async Task ExecuteAsync_WithMessageIdVariable_ApiReturnsNumber_SavesIdToSession()
    {
        var (useCase, _) = CreateUseCase("""{"response": 99999}""");
        var node = new VkRemoveKeyboardNode(
            Guid.NewGuid(),
            "Готово",
            useCase,
            messageIdVariable: "lastMsgId");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables["lastMsgId"]).IsEqualTo("99999");
    }

    [Test]
    public async Task ExecuteAsync_WithoutMessageIdVariable_DoesNotWriteAnything()
    {
        var (useCase, _) = CreateUseCase("""{"response": 77777}""");
        var node = new VkRemoveKeyboardNode(
            Guid.NewGuid(),
            "Готово",
            useCase);
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteAsync_ResolvesTextVariables()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 1}""");
        var node = new VkRemoveKeyboardNode(
            Guid.NewGuid(),
            "Готово, {name}",
            useCase);
        var context = CreateContext();
        context.Session.Variables["name"] = "Иван";

        await node.ExecuteAsync(context);

        var form = HttpUtility.ParseQueryString(capture.Body!);
        await Assert.That(form["message"]).IsEqualTo("Готово, Иван");
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
