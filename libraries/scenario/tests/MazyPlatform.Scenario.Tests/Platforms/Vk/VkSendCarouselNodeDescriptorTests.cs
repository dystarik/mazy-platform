namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net.Http;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.Nodes;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

public class VkSendCarouselNodeDescriptorTests
{
    [Test]
    public async Task Validate_WithSingleCard_ReturnsNoErrors()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {"title":"Товар","description":"Описание","photoId":"123_456"}
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithCardMissingTitle_ReturnsError()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {"description":"Описание"}
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"cards[0]\"");
        await Assert.That(errors[0]).Contains("\"title\"");
    }

    [Test]
    public async Task Validate_WithCardMissingDescription_ReturnsError()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {"title":"Товар"}
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"cards[0]\"");
        await Assert.That(errors[0]).Contains("\"description\"");
    }

    [Test]
    public async Task Validate_WithoutPhotoId_ReturnsNoErrors()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {"title":"Товар","description":"Описание"}
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithSnakeCasePhotoId_ReturnsNoErrorsButFieldIgnored()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {"title":"Товар","description":"Описание","photo_id":"123_456"}
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithButtonMissingLabel_ReturnsErrorWithButtonPath()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {
                  "title":"Товар",
                  "description":"Описание",
                  "buttons":[{"payload":"buy"}]
                }
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"cards[0].buttons[0]\"");
        await Assert.That(errors[0]).Contains("\"label\"");
    }

    [Test]
    public async Task Validate_WithButtonMissingPayload_ReturnsErrorWithButtonPath()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {
                  "title":"Товар",
                  "description":"Описание",
                  "buttons":[{"label":"Купить"}]
                }
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"cards[0].buttons[0]\"");
        await Assert.That(errors[0]).Contains("\"payload\"");
    }

    [Test]
    public async Task Validate_WithEmptyCardsArray_ReturnsNoErrors()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            { "text": "Каталог:", "cards": [] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ReturnsVkSendCarouselNode()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [
                {"title":"Товар","description":"Описание","photoId":"123_456"}
              ]
            }
            """);
        var nodeId = Guid.NewGuid();
        var services = CreateServiceProvider();

        var node = descriptor.Create(nodeId, parameters, services);

        await Assert.That(node).IsTypeOf<VkSendCarouselNode>();
        await Assert.That(node.NodeId).IsEqualTo(nodeId);
        await Assert.That(node.NodeType).IsEqualTo("vk_send_carousel");
    }

    [Test]
    public async Task Schema_HasOptionalMessageIdVariable()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();

        var param = descriptor.Schema.Single(
            p => string.Equals(p.Key, "messageIdVariable", StringComparison.Ordinal));

        await Assert.That(param.Type).IsEqualTo(NodeParamType.String);
        await Assert.That(param.IsRequired).IsFalse();
        await Assert.That(param.Description).IsNotNull();
        await Assert.That(param.Description).IsNotEmpty();
    }

    [Test]
    public async Task Validate_WithMessageIdVariable_ReturnsNoErrors()
    {
        var descriptor = new VkSendCarouselNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Каталог:",
              "cards": [{"title":"T","description":"D"}],
              "messageIdVariable": "lastId"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

    private static IServiceProvider CreateServiceProvider()
    {
        var apiClient = new VkApiClient(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new VkOptions()));
        var useCase = new VkSendCarouselUseCase(apiClient);

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(VkSendCarouselUseCase)).Returns(useCase);
        return services;
    }
}
