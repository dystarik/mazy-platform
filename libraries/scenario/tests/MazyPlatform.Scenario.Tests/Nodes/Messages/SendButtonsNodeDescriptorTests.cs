namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;

using NSubstitute;

public class SendButtonsNodeDescriptorTests
{
    [Test]
    public async Task Schema_ButtonsParameter_IsObjectMatrix()
    {
        var descriptor = new SendButtonsNodeDescriptor();

        var buttons = descriptor.Schema.Single(param => string.Equals(param.Key, "buttons", StringComparison.Ordinal));

        await Assert.That(buttons.Type).IsEqualTo(NodeParamType.ObjectMatrix);
    }

    [Test]
    public async Task Validate_WithValidButtonRows_ReturnsNoErrors()
    {
        var descriptor = new SendButtonsNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выберите действие",
              "buttons": [
                [
                  {"label": "Да", "payload": "yes", "style": "success"},
                  {"label": "Нет", "payload": "no", "style": "danger"}
                ],
                [
                  {"label": "Назад", "payload": "back", "style": "secondary"}
                ]
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithFlatButtons_ReturnsMatrixTypeError()
    {
        var descriptor = new SendButtonsNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выберите действие",
              "buttons": [{"label": "Да", "payload": "yes"}]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0]\"");
        await Assert.That(errors[0]).Contains("массив объектов");
    }

    [Test]
    public async Task Validate_WithInvalidStyle_ReturnsEnumError()
    {
        var descriptor = new SendButtonsNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выберите действие",
              "buttons": [[{"label": "Да", "payload": "yes", "style": "warning"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("primary, secondary, success, danger");
    }

    [Test]
    public async Task Create_ParsesButtonRows()
    {
        var descriptor = new SendButtonsNodeDescriptor();
        var parameters = ParseJson("""
            {
              "text": "Выберите действие",
              "buttons": [[{"label": "Да", "payload": "yes", "style": "primary"}]]
            }
            """);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ISendButtonsUseCase)).Returns(Substitute.For<ISendButtonsUseCase>());

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<SendButtonsNode>();
        await Assert.That(node.NodeType).IsEqualTo("send_buttons");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}
