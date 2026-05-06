namespace MazyPlatform.Scenario.Tests.Platforms.Vk;

using System.Net.Http;
using System.Text.Json;

using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.Nodes;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.Options;

using NSubstitute;

public class VkSendKeyboardNodeDescriptorTests
{
    [Test]
    public async Task Validate_WithSingleRowSingleButton_ReturnsNoErrors()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [[{"label":"Да","payload":"yes"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithTwoRowsTwoButtonsEach_ReturnsNoErrors()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [
                [{"label":"Да","payload":"yes","color":"positive"}, {"label":"Нет","payload":"no","color":"negative"}],
                [{"label":"Назад","payload":"back","color":"secondary"}, {"label":"Помощь","payload":"help","color":"primary"}]
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithButtonMissingLabel_ReturnsError()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [[{"payload":"yes"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0][0]\"");
        await Assert.That(errors[0]).Contains("\"label\"");
    }

    [Test]
    public async Task Validate_WithButtonMissingPayload_ReturnsError()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [[{"label":"Да"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0][0]\"");
        await Assert.That(errors[0]).Contains("\"payload\"");
    }

    [Test]
    public async Task Validate_WithUnknownColor_ReturnsEnumError()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [[{"label":"Да","payload":"yes","color":"rainbow"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0][0].color\"");
        await Assert.That(errors[0]).Contains("primary");
        await Assert.That(errors[0]).Contains("positive");
    }

    [Test]
    public async Task Validate_WithKnownColor_ReturnsNoErrors()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [[{"label":"Да","payload":"yes","color":"positive"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ReturnsVkSendKeyboardNode()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выбери:",
              "buttons": [[{"label":"Да","payload":"yes"}]],
              "oneTime": true
            }
            """);
        var nodeId = Guid.NewGuid();
        var services = CreateServiceProvider();

        var node = descriptor.Create(nodeId, parameters, services);

        await Assert.That(node).IsTypeOf<VkSendKeyboardNode>();
        await Assert.That(node.NodeId).IsEqualTo(nodeId);
        await Assert.That(node.NodeType).IsEqualTo("vk_send_keyboard");
    }

    [Test]
    public async Task Schema_DoesNotExposeInlineParameter()
    {
        var descriptor = new VkSendKeyboardNodeDescriptor();
        var keys = descriptor.Schema.Select(static param => param.Key).ToList();

        await Assert.That(keys).DoesNotContain("inline");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

    private static IServiceProvider CreateServiceProvider()
    {
        var apiClient = new VkApiClient(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new VkOptions()));
        var useCase = new VkSendKeyboardUseCase(apiClient);

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(VkSendKeyboardUseCase)).Returns(useCase);
        return services;
    }
}
