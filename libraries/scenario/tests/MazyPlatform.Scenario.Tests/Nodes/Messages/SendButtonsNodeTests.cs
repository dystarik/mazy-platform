namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class SendButtonsNodeTests
{
    [Test]
    public async Task ExecuteAsync_ReplacesVariablesInButtonLabels()
    {
        var sendUseCase = Substitute.For<ISendButtonsUseCase>();
        ButtonLayout? capturedButtons = null;
        sendUseCase
            .ExecuteAsync(
                Arg.Any<ExecutionContext>(),
                Arg.Any<string>(),
                Arg.Any<ButtonLayout>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedButtons = callInfo.ArgAt<ButtonLayout>(2);
                return new SendResult(Substitute.For<IOutgoingAction>(), null);
            });

        var node = new SendButtonsNode(
            Guid.NewGuid(),
            "Выберите действие",
            new ButtonLayout(
            [
                [new ButtonDefinition("Подтвердить заказ в {address}", "confirm_order", ButtonStyle.Success)],
                [new ButtonDefinition("Назад", "back", ButtonStyle.Secondary)],
            ]),
            sendUseCase);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["address"] = "ул. Ленина, 10" });

        await node.ExecuteAsync(context);

        await Assert.That(capturedButtons).IsNotNull();
        await Assert.That(capturedButtons!.Rows[0][0].Label).IsEqualTo("Подтвердить заказ в ул. Ленина, 10");
        await Assert.That(capturedButtons.Rows[0][0].Payload).IsEqualTo("confirm_order");
        await Assert.That(capturedButtons.Rows[0][0].Style).IsEqualTo(ButtonStyle.Success);
        await Assert.That(capturedButtons.Rows[1][0].Label).IsEqualTo("Назад");
    }

    [Test]
    public async Task ExecuteAsync_UnknownVariableInButtonLabel_RemainsUnchanged()
    {
        var sendUseCase = Substitute.For<ISendButtonsUseCase>();
        ButtonLayout? capturedButtons = null;
        sendUseCase
            .ExecuteAsync(
                Arg.Any<ExecutionContext>(),
                Arg.Any<string>(),
                Arg.Any<ButtonLayout>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedButtons = callInfo.ArgAt<ButtonLayout>(2);
                return new SendResult(Substitute.For<IOutgoingAction>(), null);
            });

        var node = new SendButtonsNode(
            Guid.NewGuid(),
            "Выберите действие",
            new ButtonLayout([[new ButtonDefinition("Подтвердить заказ в {unknown}", "confirm_order")]]),
            sendUseCase);

        await node.ExecuteAsync(CreateContext());

        await Assert.That(capturedButtons).IsNotNull();
        await Assert.That(capturedButtons!.Rows[0][0].Label).IsEqualTo("Подтвердить заказ в {unknown}");
        await Assert.That(capturedButtons.Rows[0][0].Payload).IsEqualTo("confirm_order");
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
