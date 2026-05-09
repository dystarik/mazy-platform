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

public class VkDeleteMessageUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_WithConversationMessageId_UsesCmidsWithPeerId()
    {
        var (useCase, capture) = CreateUseCase("""{"response": {"42": 1}}""");

        await useCase.ExecuteAsync(CreateContext(), "vk:cmid:42");

        var form = HttpUtility.ParseQueryString(capture.Requests[0].Body!);
        await Assert.That(capture.Requests[0].RequestUri!.AbsoluteUri).EndsWith("/messages.delete");
        await Assert.That(form["cmids"]).IsEqualTo("42");
        await Assert.That(form["peer_id"]).IsEqualTo("2000000001");
        await Assert.That(form["delete_for_all"]).IsEqualTo("1");
        await Assert.That(form["message_ids"]).IsNull();
        await Assert.That(capture.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_WithMessageId_UsesMessageIds()
    {
        var (useCase, capture) = CreateUseCase("""{"response": {"123": 1}}""");

        await useCase.ExecuteAsync(CreateContext(), "vk:mid:123");

        var form = HttpUtility.ParseQueryString(capture.Requests[0].Body!);
        await Assert.That(form["message_ids"]).IsEqualTo("123");
        await Assert.That(form["cmids"]).IsNull();
        await Assert.That(form["peer_id"]).IsNull();
        await Assert.That(form["delete_for_all"]).IsEqualTo("1");
        await Assert.That(capture.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_WithLegacyId_FallsBackToConversationMessageId()
    {
        var (useCase, capture) = CreateUseCase(
            """{"response": {"42": 0}}""",
            """{"response": {"42": 1}}""");

        await useCase.ExecuteAsync(CreateContext(), "42");

        var firstForm = HttpUtility.ParseQueryString(capture.Requests[0].Body!);
        var secondForm = HttpUtility.ParseQueryString(capture.Requests[1].Body!);

        await Assert.That(firstForm["message_ids"]).IsEqualTo("42");
        await Assert.That(firstForm["cmids"]).IsNull();
        await Assert.That(firstForm["peer_id"]).IsNull();
        await Assert.That(secondForm["cmids"]).IsEqualTo("42");
        await Assert.That(secondForm["peer_id"]).IsEqualTo("2000000001");
    }

    [Test]
    public async Task ExecuteAsync_WhenVkDoesNotDeleteMessage_DoesNotThrow()
    {
        var (useCase, capture) = CreateUseCase(
            """{"response": {"42": 0}}""",
            """{"response": {"42": 0}}""");

        var action = await useCase.ExecuteAsync(CreateContext(), "42");

        await Assert.That(action).IsTypeOf<DeleteMessageAction>();
        await Assert.That(capture.Requests.Count).IsEqualTo(2);
    }

    private static (VkDeleteMessageUseCase UseCase, RequestCapture Capture) CreateUseCase(params string[] vkResponseJson)
    {
        var capture = new RequestCapture();
        var handler = new CapturingHandler(HttpStatusCode.OK, vkResponseJson, capture);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        var apiClient = new VkApiClient(factory, Options.Create(new VkOptions()));
        return (new VkDeleteMessageUseCase(apiClient), capture);
    }

    private static ExecutionContext CreateContext() =>
        new()
        {
            Session = new TestSession(),
            IncomingEvent = new TestIncomingEvent { ChatId = "2000000001" },
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = "test_token",
            ProjectId = Guid.NewGuid(),
        };

    private sealed class RequestCapture
    {
        public List<CapturedRequest> Requests { get; } = [];
    }

    private sealed class CapturedRequest
    {
        public Uri? RequestUri { get; init; }

        public string? Body { get; init; }
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, IReadOnlyList<string> responseBodies, RequestCapture capture) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            capture.Requests.Add(new CapturedRequest
            {
                RequestUri = request.RequestUri,
                Body = body,
            });

            var responseBody = responseBodies[Math.Min(capture.Requests.Count - 1, responseBodies.Count - 1)];

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody),
            };
        }
    }
}
