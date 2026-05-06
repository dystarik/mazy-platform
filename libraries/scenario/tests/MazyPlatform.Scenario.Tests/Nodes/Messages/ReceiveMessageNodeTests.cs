namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Abstractions.Validation;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ReceiveMessageNodeTests
{
    [Test]
    public async Task ExecuteAsync_WithoutEvent_ReturnsWait()
    {
        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "input",
            Substitute.For<IReceiveMessageUseCase>(),
            Substitute.For<ISendMessageUseCase>(),
            new Dictionary<string, IInputValidator>(StringComparer.Ordinal));

        var context = CreateContext(null);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
    }

    [Test]
    public async Task ExecuteAsync_CanHandleFalse_ReturnsWait()
    {
        var incomingEvent = new TestIncomingEvent { Text = "hello" };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(false);

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "input",
            receiveUseCase,
            Substitute.For<ISendMessageUseCase>(),
            new Dictionary<string, IInputValidator>(StringComparer.Ordinal));

        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
    }

    [Test]
    public async Task ExecuteAsync_SavesTextToVariable()
    {
        var incomingEvent = new TestIncomingEvent { Text = "мой текст" };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(true);
        receiveUseCase.ExtractText(incomingEvent).Returns("мой текст");

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "input",
            receiveUseCase,
            Substitute.For<ISendMessageUseCase>(),
            new Dictionary<string, IInputValidator>(StringComparer.Ordinal));

        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["input"]).IsEqualTo("мой текст");
    }

    [Test]
    public async Task ExecuteAsync_WithMessageIdVariable_SavesTextAndMessageId()
    {
        var incomingEvent = new TestIncomingEvent { Text = "мой текст", MessageId = "42" };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(true);
        receiveUseCase.ExtractText(incomingEvent).Returns("мой текст");

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "input",
            receiveUseCase,
            Substitute.For<ISendMessageUseCase>(),
            new Dictionary<string, IInputValidator>(StringComparer.Ordinal),
            messageIdVariable: "incomingMessageId");

        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["input"]).IsEqualTo("мой текст");
        await Assert.That(context.Session.Variables["incomingMessageId"]).IsEqualTo("42");
    }

    [Test]
    public async Task ExecuteAsync_WithMessageIdVariableAndNullMessageId_DoesNotCreateMessageIdVariable()
    {
        var incomingEvent = new TestIncomingEvent { Text = "мой текст", MessageId = null };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(true);
        receiveUseCase.ExtractText(incomingEvent).Returns("мой текст");

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "input",
            receiveUseCase,
            Substitute.For<ISendMessageUseCase>(),
            new Dictionary<string, IInputValidator>(StringComparer.Ordinal),
            messageIdVariable: "incomingMessageId");

        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["input"]).IsEqualTo("мой текст");
        await Assert.That(context.Session.Variables.ContainsKey("incomingMessageId")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_InvalidInput_ReturnsWaitWithError()
    {
        var incomingEvent = new TestIncomingEvent { Text = "invalid" };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(true);
        receiveUseCase.ExtractText(incomingEvent).Returns("invalid");

        var validator = Substitute.For<IInputValidator>();
        validator
            .IsValid("invalid", Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns(false);

        var validators = new Dictionary<string, IInputValidator>(StringComparer.Ordinal)
        {
            ["phone"] = validator,
        };

        var sendUseCase = Substitute.For<ISendMessageUseCase>();
        sendUseCase
            .ExecuteAsync(Arg.Any<ExecutionContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SendResult(Substitute.For<IOutgoingAction>(), null));

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "input",
            receiveUseCase,
            sendUseCase,
            validators,
            validatorType: "phone",
            messageIdVariable: "incomingMessageId");

        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.WaitForEvent);
        await Assert.That(result.Actions).IsNotNull();
        await Assert.That(result.Actions!.Count).IsEqualTo(1);
        await Assert.That(context.Session.Variables.ContainsKey("incomingMessageId")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_ValidInput_SavesAndContinues()
    {
        var incomingEvent = new TestIncomingEvent { Text = "+79991234567" };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(true);
        receiveUseCase.ExtractText(incomingEvent).Returns("+79991234567");

        var validator = Substitute.For<IInputValidator>();
        validator
            .IsValid("+79991234567", Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns(true);

        var validators = new Dictionary<string, IInputValidator>(StringComparer.Ordinal)
        {
            ["phone"] = validator,
        };

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "phone",
            receiveUseCase,
            Substitute.For<ISendMessageUseCase>(),
            validators,
            validatorType: "phone");

        var context = CreateContext(incomingEvent);

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(context.Session.Variables["phone"]).IsEqualTo("+79991234567");
    }

    [Test]
    public async Task ExecuteAsync_CustomErrorMessage()
    {
        var incomingEvent = new TestIncomingEvent { Text = "bad" };
        var receiveUseCase = Substitute.For<IReceiveMessageUseCase>();
        receiveUseCase.CanHandle(incomingEvent).Returns(true);
        receiveUseCase.ExtractText(incomingEvent).Returns("bad");

        var validator = Substitute.For<IInputValidator>();
        validator
            .IsValid("bad", Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns(false);

        var validators = new Dictionary<string, IInputValidator>(StringComparer.Ordinal)
        {
            ["email"] = validator,
        };

        string? sentMessage = null;
        var sendUseCase = Substitute.For<ISendMessageUseCase>();
        sendUseCase
            .ExecuteAsync(Arg.Any<ExecutionContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                sentMessage = callInfo.ArgAt<string>(1);
                return new SendResult(Substitute.For<IOutgoingAction>(), null!);
            });

        var node = new ReceiveMessageNode(
            Guid.NewGuid(),
            "email",
            receiveUseCase,
            sendUseCase,
            validators,
            validatorType: "email",
            errorMessage: "Введите корректный email.");

        var context = CreateContext(incomingEvent);

        await node.ExecuteAsync(context);

        await Assert.That(sentMessage).IsEqualTo("Введите корректный email.");
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
