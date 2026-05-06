namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;

using NSubstitute;

public class EditMessageNodeDescriptorTests
{
    [Test]
    public async Task Schema_HasButtonsKey_NotNewButtons()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("buttons");
        await Assert.That(keys).DoesNotContain("newButtons");
    }

    [Test]
    public async Task Validate_WithValidButtons_ReturnsNoErrors()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "Обновлённый текст",
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
    public async Task Validate_ButtonMissingLabel_ReturnsErrorWithButtonPath()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "x",
              "buttons": [[{"payload": "yes"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"buttons[0][0]\"");
        await Assert.That(errors[0]).Contains("\"label\"");
    }

    [Test]
    public async Task Validate_WithFlatButtons_ReturnsMatrixTypeError()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "x",
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
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "x",
              "buttons": [[{"label": "Да", "payload": "yes", "style": "warning"}]]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("primary, secondary, success, danger");
    }

    [Test]
    public async Task Validate_WithEmptyButtons_ReturnsNoErrors()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "x",
              "buttons": []
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_ParsesButtons()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "Обновлённый",
              "buttons": [
                [
                  {"label": "Да", "payload": "yes", "style": "primary"},
                  {"label": "Нет", "payload": "no", "style": "danger"}
                ]
              ]
            }
            """);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEditMessageUseCase)).Returns(Substitute.For<IEditMessageUseCase>());

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<EditMessageNode>();
        await Assert.That(node.NodeType).IsEqualTo("edit_message");
    }

    [Test]
    public async Task Create_WithoutButtonsKey_ProducesNullButtons()
    {
        var descriptor = new EditMessageNodeDescriptor();
        var parameters = ParseJson("""
            {
              "messageIdVariable": "lastMessageId",
              "newText": "x"
            }
            """);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEditMessageUseCase)).Returns(Substitute.For<IEditMessageUseCase>());

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);

        await Assert.That(node).IsTypeOf<EditMessageNode>();
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}
