namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;

using NSubstitute;

public class ReceiveButtonPressNodeDescriptorTests
{
    [Test]
    public async Task Schema_HasButtonPayloadVariable_NotVariable()
    {
        var descriptor = new ReceiveButtonPressNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("buttonPayloadVariable");
        await Assert.That(keys).DoesNotContain("variable");
    }

    [Test]
    public async Task Schema_ButtonPayloadVariable_IsRequiredString()
    {
        var descriptor = new ReceiveButtonPressNodeDescriptor();

        var param = descriptor.Schema.Single(
            p => string.Equals(p.Key, "buttonPayloadVariable", StringComparison.Ordinal));

        await Assert.That(param.IsRequired).IsTrue();
        await Assert.That(param.Type).IsEqualTo(NodeParamType.String);
        await Assert.That(param.Description).IsNotNull();
        await Assert.That(param.Description).IsNotEmpty();
    }

    [Test]
    public async Task Validate_WithValidParameters_ReturnsNoErrors()
    {
        var descriptor = new ReceiveButtonPressNodeDescriptor();
        var parameters = ParseJson("""
            { "buttonPayloadVariable": "lastButton", "expectedPayloads": ["yes", "no"] }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ReturnsReceiveButtonPressNode()
    {
        var descriptor = new ReceiveButtonPressNodeDescriptor();
        var parameters = ParseJson("""
            { "buttonPayloadVariable": "lastButton", "expectedPayloads": ["yes", "no"], "isEntry": true }
            """);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IReceiveButtonPressUseCase))
            .Returns(Substitute.For<IReceiveButtonPressUseCase>());

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<ReceiveButtonPressNode>();
        await Assert.That(node.NodeType).IsEqualTo("receive_button_press");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}
