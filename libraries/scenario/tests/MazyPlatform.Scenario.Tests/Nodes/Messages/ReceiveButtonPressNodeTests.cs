namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ReceiveButtonPressNodeTests
{
    [Test]
    public async Task ExecuteAsync_WithoutEvent_ReturnsWait()
    {
        var node = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            Substitute.For<IReceiveButtonPressUseCase>());

        var result = await node.ExecuteAsync(CreateContext(null));

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
    }

    [Test]
    public async Task ExecuteAsync_PayloadNotInList_ReturnsWait()
    {
        var incomingEvent = new TestIncomingEvent { Payload = "unknown" };
        var useCase = Substitute.For<IReceiveButtonPressUseCase>();
        useCase.CanHandle(incomingEvent).Returns(true);
        useCase.ExtractPayload(incomingEvent).Returns("unknown");

        var node = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            useCase,
            ["yes", "no"]);
        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
    }

    [Test]
    public async Task ExecuteAsync_SavesPayload_AndContinues()
    {
        var incomingEvent = new TestIncomingEvent { Payload = "yes" };
        var useCase = Substitute.For<IReceiveButtonPressUseCase>();
        useCase.CanHandle(incomingEvent).Returns(true);
        useCase.ExtractPayload(incomingEvent).Returns("yes");

        var node = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            useCase,
            ["yes", "no"]);
        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["btn"]).IsEqualTo("yes");
    }

    [Test]
    public async Task IsEntry_DefaultsToFalse()
    {
        var node = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            Substitute.For<IReceiveButtonPressUseCase>());

        await Assert.That(node.IsEntry).IsFalse();
    }

    [Test]
    public async Task IsEntry_SetViaConstructor_IsTrue()
    {
        var node = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            Substitute.For<IReceiveButtonPressUseCase>(),
            ["menu"],
            isEntry: true);

        await Assert.That(node.IsEntry).IsTrue();
    }

    [Test]
    public async Task ExpectedPayloads_AccessibleViaProperty()
    {
        var node = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            Substitute.For<IReceiveButtonPressUseCase>(),
            ["yes", "no"]);

        await Assert.That(node.ExpectedPayloads).IsNotNull();
        await Assert.That(node.ExpectedPayloads!.Count).IsEqualTo(2);
        await Assert.That(node.ExpectedPayloads[0]).IsEqualTo("yes");
        await Assert.That(node.ExpectedPayloads[1]).IsEqualTo("no");
    }

    private static ExecutionContext CreateContext(TestIncomingEvent? incomingEvent)
    {
        return new ExecutionContext
        {
            Session = new TestSession(),
            IncomingEvent = incomingEvent,
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
