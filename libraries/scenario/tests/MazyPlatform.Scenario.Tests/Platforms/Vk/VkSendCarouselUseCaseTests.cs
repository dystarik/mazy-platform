namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net;
using System.Net.Http;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Tests.Helpers;
using MazyPlatform.Scenario.Vk.Actions;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

using ExecutionContext = MazyPlatform.Scenario.Abstractions.Execution.ExecutionContext;

public class VkSendCarouselUseCaseTests
{
    [Test]
    public async Task ExecuteAsync_ApiReturnsNumber_ReturnsMessageId()
    {
        var useCase = CreateUseCase("""{"response": 12345}""");
        var card = new VkCarouselCard("Title", "Desc", PhotoId: null, Buttons: []);

        var result = await useCase.ExecuteAsync(CreateContext(), [card], "Каталог:");

        await Assert.That(result.MessageId).IsEqualTo("12345");
        await Assert.That(result.Action).IsTypeOf<VkSendCarouselAction>();
    }

    [Test]
    public async Task ExecuteAsync_ApiReturnsObject_ReturnsNullMessageId()
    {
        var useCase = CreateUseCase("""{"response": {"id": 1}}""");
        var card = new VkCarouselCard("Title", "Desc", PhotoId: null, Buttons: []);

        var result = await useCase.ExecuteAsync(CreateContext(), [card], "Каталог:");

        await Assert.That(result.MessageId).IsNull();
        await Assert.That(result.Action).IsTypeOf<VkSendCarouselAction>();
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
