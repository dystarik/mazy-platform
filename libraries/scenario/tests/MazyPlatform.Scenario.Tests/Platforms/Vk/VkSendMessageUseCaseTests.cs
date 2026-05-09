namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
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

public class VkSendMessageUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_UsesPeerIdAndReturnsMessageId()
    {
        var (useCase, capture) = CreateUseCase("""{"response": 12345}""");

        var result = await useCase.ExecuteAsync(CreateContext(), "Привет");

        var form = HttpUtility.ParseQueryString(capture.Body!);
        await Assert.That(form["peer_id"]).IsEqualTo("test_chat");
        await Assert.That(form["peer_ids"]).IsNull();
        await Assert.That(form["message"]).IsEqualTo("Привет");
        await Assert.That(result.MessageId).IsEqualTo("vk:mid:12345");
        await Assert.That(result.Action).IsTypeOf<SendTextAction>();
    }

    private static (VkSendMessageUseCase UseCase, RequestCapture Capture) CreateUseCase(string vkResponseJson)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(HttpStatusCode.OK, vkResponseJson, capture);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        var apiClient = new VkApiClient(factory, Options.Create(new VkOptions()));
        return (new VkSendMessageUseCase(apiClient), capture);
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
