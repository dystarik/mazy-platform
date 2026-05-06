namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
using System.Net.Http;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Tests.Helpers;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.Nodes;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class VkSendCarouselNodeTests
{
    [Test]
    public async Task ExecuteAsync_WithMessageIdVariable_ApiReturnsNumber_SavesIdToSession()
    {
        var useCase = CreateUseCase("""{"response": 99999}""");
        var card = new VkCarouselCard("T", "D", null, []);
        var node = new VkSendCarouselNode(
            Guid.NewGuid(),
            [card],
            useCase,
            "Каталог:",
            messageIdVariable: "lastCarouselId");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables["lastCarouselId"]).IsEqualTo("99999");
    }

    [Test]
    public async Task ExecuteAsync_WithMessageIdVariable_ApiReturnsNonNumber_DoesNotCreateVariable()
    {
        var useCase = CreateUseCase("""{"response": {"id": 1}}""");
        var card = new VkCarouselCard("T", "D", null, []);
        var node = new VkSendCarouselNode(
            Guid.NewGuid(),
            [card],
            useCase,
            "Каталог:",
            messageIdVariable: "lastCarouselId");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables.ContainsKey("lastCarouselId")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_WithoutMessageIdVariable_DoesNotWriteAnything()
    {
        var useCase = CreateUseCase("""{"response": 77777}""");
        var card = new VkCarouselCard("T", "D", null, []);
        var node = new VkSendCarouselNode(
            Guid.NewGuid(),
            [card],
            useCase,
            "Каталог:");
        var context = CreateContext();

        await node.ExecuteAsync(context);

        await Assert.That(context.Session.Variables.Count).IsEqualTo(0);
    }

    private static VkSendCarouselUseCase CreateUseCase(string vkResponseJson)
    {
        var handler = new StubHandler(HttpStatusCode.OK, vkResponseJson);
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);

        var apiClient = new VkApiClient(factory, Options.Create(new VkOptions()));
        return new VkSendCarouselUseCase(apiClient);
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

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            });
    }
}
