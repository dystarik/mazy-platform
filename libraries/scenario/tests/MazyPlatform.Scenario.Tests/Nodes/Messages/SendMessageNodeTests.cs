namespace MazyPlatform.Scenario.Tests.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class SendMessageNodeTests
{
    [Test]
    public async Task ExecuteAsync_SendsTextViaUseCase()
    {
        var sendUseCase = Substitute.For<ISendMessageUseCase>();
        var action = Substitute.For<IOutgoingAction>();
        sendUseCase
            .ExecuteAsync(Arg.Any<ExecutionContext>(), "Привет!", Arg.Any<CancellationToken>())
            .Returns(new SendResult(Substitute.For<IOutgoingAction>(), null));

        var node = new SendMessageNode(Guid.NewGuid(), "Привет!", sendUseCase);
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
        await Assert.That(result.Actions!.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_ReplacesVariablesInText()
    {
        var sendUseCase = Substitute.For<ISendMessageUseCase>();
        string? capturedText = null;
        sendUseCase
            .ExecuteAsync(Arg.Any<ExecutionContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedText = callInfo.ArgAt<string>(1);
                return new SendResult(Substitute.For<IOutgoingAction>(), null);
            });

        var node = new SendMessageNode(Guid.NewGuid(), "Привет, {name}!", sendUseCase);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" });

        await node.ExecuteAsync(context);

        await Assert.That(capturedText).IsEqualTo("Привет, Иван!");
    }

    [Test]
    public async Task ExecuteAsync_ReturnsContinue()
    {
        var sendUseCase = Substitute.For<ISendMessageUseCase>();
        sendUseCase
            .ExecuteAsync(Arg.Any<ExecutionContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SendResult(Substitute.For<IOutgoingAction>(), null));

        var node = new SendMessageNode(Guid.NewGuid(), "текст", sendUseCase);
        var context = CreateContext();

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.Status).IsEqualTo(NodeExecutionStatus.Continue);
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
