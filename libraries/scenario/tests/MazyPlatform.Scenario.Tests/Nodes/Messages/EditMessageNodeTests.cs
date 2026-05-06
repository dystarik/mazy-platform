namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class EditMessageNodeTests
{
    [Test]
    public async Task ExecuteAsync_ReplacesVariablesInButtonLabels()
    {
        var editUseCase = Substitute.For<IEditMessageUseCase>();
        ButtonLayout? capturedButtons = null;
        string? capturedMessageId = null;
        editUseCase
            .ExecuteAsync(
                Arg.Any<ExecutionContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ButtonLayout?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedMessageId = callInfo.ArgAt<string>(1);
                capturedButtons = callInfo.ArgAt<ButtonLayout?>(3);
                return Substitute.For<IOutgoingAction>();
            });

        var node = new EditMessageNode(
            Guid.NewGuid(),
            "lastMessageId",
            "Обновлено",
            new ButtonLayout([[new ButtonDefinition("Доставить в {address}", "delivery_confirm", ButtonStyle.Primary)]]),
            editUseCase);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["lastMessageId"] = "42",
                ["address"] = "ул. Ленина, 10",
            });

        await node.ExecuteAsync(context);

        await Assert.That(capturedMessageId).IsEqualTo("42");
        await Assert.That(capturedButtons).IsNotNull();
        await Assert.That(capturedButtons!.Rows[0][0].Label).IsEqualTo("Доставить в ул. Ленина, 10");
        await Assert.That(capturedButtons.Rows[0][0].Payload).IsEqualTo("delivery_confirm");
        await Assert.That(capturedButtons.Rows[0][0].Style).IsEqualTo(ButtonStyle.Primary);
    }

    [Test]
    public async Task ExecuteAsync_WithNullButtons_PassesNullToUseCase()
    {
        var editUseCase = Substitute.For<IEditMessageUseCase>();
        ButtonLayout? capturedButtons = new([[new ButtonDefinition("old", "old")]]);
        editUseCase
            .ExecuteAsync(
                Arg.Any<ExecutionContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ButtonLayout?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedButtons = callInfo.ArgAt<ButtonLayout?>(3);
                return Substitute.For<IOutgoingAction>();
            });

        var node = new EditMessageNode(
            Guid.NewGuid(),
            "lastMessageId",
            "Обновлено",
            null,
            editUseCase);

        await node.ExecuteAsync(CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["lastMessageId"] = "42" }));

        await Assert.That(capturedButtons).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyButtons_PassesEmptyLayoutToUseCase()
    {
        var editUseCase = Substitute.For<IEditMessageUseCase>();
        ButtonLayout? capturedButtons = null;
        editUseCase
            .ExecuteAsync(
                Arg.Any<ExecutionContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ButtonLayout?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedButtons = callInfo.ArgAt<ButtonLayout?>(3);
                return Substitute.For<IOutgoingAction>();
            });

        var node = new EditMessageNode(
            Guid.NewGuid(),
            "lastMessageId",
            "Обновлено",
            ButtonLayout.Empty,
            editUseCase);

        await node.ExecuteAsync(CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["lastMessageId"] = "42" }));

        await Assert.That(capturedButtons).IsNotNull();
        await Assert.That(capturedButtons!.Rows.Count).IsEqualTo(0);
    }

    private static ExecutionContext CreateContext(IDictionary<string, object?>? variables = null)
    {
        var session = new TestSession();

        if (variables is not null)
        {
            foreach (var (key, value) in variables)
            {
                session.Variables[key] = value;
            }
        }

        return new ExecutionContext
        {
            Session = session,
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
