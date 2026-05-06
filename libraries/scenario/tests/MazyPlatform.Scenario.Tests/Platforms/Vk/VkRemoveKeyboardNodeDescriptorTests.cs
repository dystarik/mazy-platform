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

public class VkRemoveKeyboardNodeDescriptorTests
{
    [Test]
    public async Task Schema_ContainsTextAndMessageIdVariable()
    {
        var descriptor = new VkRemoveKeyboardNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("text");
        await Assert.That(keys).Contains("messageIdVariable");
    }

    [Test]
    public async Task Schema_Text_IsRequiredStringWithDescription()
    {
        var descriptor = new VkRemoveKeyboardNodeDescriptor();

        var text = descriptor.Schema.Single(
            p => string.Equals(p.Key, "text", StringComparison.Ordinal));

        await Assert.That(text.IsRequired).IsTrue();
        await Assert.That(text.Type).IsEqualTo(NodeParamType.String);
        await Assert.That(text.Description).IsNotNull();
        await Assert.That(text.Description).IsNotEmpty();
    }

    [Test]
    public async Task Schema_MessageIdVariable_IsOptional()
    {
        var descriptor = new VkRemoveKeyboardNodeDescriptor();

        var param = descriptor.Schema.Single(
            p => string.Equals(p.Key, "messageIdVariable", StringComparison.Ordinal));

        await Assert.That(param.IsRequired).IsFalse();
        await Assert.That(param.Type).IsEqualTo(NodeParamType.String);
    }

    [Test]
    public async Task Validate_WithValidParameters_ReturnsNoErrors()
    {
        var descriptor = new VkRemoveKeyboardNodeDescriptor();
        var parameters = ParseJson("""
            { "text": "Готово", "messageIdVariable": "lastId" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_MissingText_ReturnsError()
    {
        var descriptor = new VkRemoveKeyboardNodeDescriptor();
        var parameters = ParseJson("""{ "messageIdVariable": "lastId" }""");

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"text\"");
    }

    [Test]
    public async Task Create_ReturnsVkRemoveKeyboardNode()
    {
        var descriptor = new VkRemoveKeyboardNodeDescriptor();
        var parameters = ParseJson("""{ "text": "Готово" }""");
        var services = CreateServiceProvider();

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<VkRemoveKeyboardNode>();
        await Assert.That(node.NodeType).IsEqualTo("vk_remove_keyboard");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

    private static IServiceProvider CreateServiceProvider()
    {
        var apiClient = new VkApiClient(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new VkOptions()));
        var useCase = new VkRemoveKeyboardUseCase(apiClient);

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(VkRemoveKeyboardUseCase)).Returns(useCase);
        return services;
    }
}
