namespace MazyPlatform.Scenario.Tests.Executor;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Sessions;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Executor;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ScenarioExecutorTests
{
    [Test]
    public async Task ExecuteAsync_LinearScenario_ExecutesAllNodes()
    {
        var node1 = new TestNode(
            Guid.NewGuid(),
            "a",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var node2 = new TestNode(
            Guid.NewGuid(),
            "b",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var graph = ScenarioGraphFactory.CreateLinear(node1, node2);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.Completed);
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_AwaitingNode_FirstVisit_ReturnsWaitForEvent()
    {
        var awaitingNode = new TestNode(
            Guid.NewGuid(),
            "recv",
            true,
            _ => Task.FromResult(NodeResult.Wait()));
        var graph = ScenarioGraphFactory.CreateLinear(awaitingNode);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.WaitingForEvent);
        await Assert.That(session.CurrentNodeId).IsEqualTo(awaitingNode.NodeId);
    }

    [Test]
    public async Task ExecuteAsync_AwaitingNode_ContinuesOnContinue()
    {
        var awaitingNode = new TestNode(
            Guid.NewGuid(),
            "recv",
            true,
            _ => Task.FromResult(NodeResult.Continue()));
        var nextNode = new TestNode(
            Guid.NewGuid(),
            "next",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var graph = ScenarioGraphFactory.CreateLinear(awaitingNode, nextNode);
        var session = new TestSession
        {
            CurrentNodeId = awaitingNode.NodeId,
            State = SessionState.WaitingForEvent,
        };

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.Completed);
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_NodeError_ReturnsError()
    {
        var errorNode = new TestNode(
            Guid.NewGuid(),
            "err",
            false,
            _ => Task.FromResult(NodeResult.Error("что-то сломалось")));
        var graph = ScenarioGraphFactory.CreateLinear(errorNode);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).IsEqualTo("что-то сломалось");
    }

    [Test]
    public async Task ExecuteAsync_ScenarioCompletes_WhenNoNextNode()
    {
        var node = new TestNode(
            Guid.NewGuid(),
            "single",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var graph = ScenarioGraphFactory.CreateLinear(node);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.Completed);
        await Assert.That(session.CurrentNodeId).IsNull();
    }

    [Test]
    public async Task ExecuteAsync_NodeNotFoundInGraph_ReturnsError()
    {
        var missingNodeId = Guid.NewGuid();
        var graph = new ScenarioGraph
        {
            StartNodeId = missingNodeId,
            Nodes = new Dictionary<Guid, INode>(),
            Connections = new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            EntryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal),
        };
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_CollectsActionsFromAllNodes()
    {
        var action1 = Substitute.For<IOutgoingAction>();
        var action2 = Substitute.For<IOutgoingAction>();

        var node1 = new TestNode(
            Guid.NewGuid(),
            "a",
            false,
            _ => Task.FromResult(NodeResult.Continue(action1)));
        var node2 = new TestNode(
            Guid.NewGuid(),
            "b",
            false,
            _ => Task.FromResult(NodeResult.Continue(action2)));
        var graph = ScenarioGraphFactory.CreateLinear(node1, node2);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.Actions.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ExecuteAsync_ConditionalBranching_GoesToCorrectBranch()
    {
        var conditionNode = new TestNode(
            Guid.NewGuid(),
            "cond",
            false,
            _ => Task.FromResult(NodeResult.Branch("true")));
        var trueNode = new TestNode(
            Guid.NewGuid(),
            "true_branch",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "true_branch";
                return Task.FromResult(NodeResult.Continue());
            });
        var falseNode = new TestNode(
            Guid.NewGuid(),
            "false_branch",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "false_branch";
                return Task.FromResult(NodeResult.Continue());
            });

        var nodes = new Dictionary<Guid, INode>
        {
            [conditionNode.NodeId] = conditionNode,
            [trueNode.NodeId] = trueNode,
            [falseNode.NodeId] = falseNode,
        };
        var connections = new Dictionary<Guid, IReadOnlyList<NodeConnection>>
        {
            [conditionNode.NodeId] =
            [
                new NodeConnection(conditionNode.NodeId, trueNode.NodeId, "true"),
                new NodeConnection(conditionNode.NodeId, falseNode.NodeId, "false"),
            ],
        };
        var graph = ScenarioGraphFactory.Create(conditionNode.NodeId, nodes, connections);
        var session = new TestSession();

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["visited"]).IsEqualTo("true_branch");
    }

    [Test]
    public async Task ExecuteAsync_AwaitingNode_NotFirst_StopsExecution()
    {
        var firstNode = new TestNode(
            Guid.NewGuid(),
            "first",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var awaitingNode = new TestNode(
            Guid.NewGuid(),
            "awaiting",
            true,
            _ => Task.FromResult(NodeResult.Wait()));
        var graph = ScenarioGraphFactory.CreateLinear(firstNode, awaitingNode);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.WaitingForEvent);
        await Assert.That(session.CurrentNodeId).IsEqualTo(awaitingNode.NodeId);
    }

    [Test]
    public async Task ExecuteAsync_NonAwaitingNodeWaitResult_StopsExecution()
    {
        var firstNode = new TestNode(
            Guid.NewGuid(),
            "first",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var waitingNode = new TestNode(
            Guid.NewGuid(),
            "wait",
            false,
            _ => Task.FromResult(NodeResult.Wait()));
        var nextNode = new TestNode(
            Guid.NewGuid(),
            "next",
            false,
            context =>
            {
                context.Session.Variables["next"] = "visited";
                return Task.FromResult(NodeResult.Continue());
            });
        var graph = ScenarioGraphFactory.CreateLinear(firstNode, waitingNode, nextNode);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.WaitingForEvent);
        await Assert.That(session.CurrentNodeId).IsEqualTo(waitingNode.NodeId);
        await Assert.That(session.Variables.ContainsKey("next")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_DelayNode_FirstVisit_SavesResumeAtAndStopsExecution()
    {
        var delayNode = new DelayNode(Guid.NewGuid(), 60);
        var nextNode = new TestNode(
            Guid.NewGuid(),
            "next",
            false,
            context =>
            {
                context.Session.Variables["next"] = "visited";
                return Task.FromResult(NodeResult.Continue());
            });
        var graph = ScenarioGraphFactory.CreateLinear(delayNode, nextNode);
        var session = new TestSession();

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.WaitingForEvent);
        await Assert.That(session.CurrentNodeId).IsEqualTo(delayNode.NodeId);
        await Assert.That(session.Variables.ContainsKey("__delay_resume_at")).IsTrue();
        await Assert.That(session.Variables.ContainsKey("next")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_DelayNode_AfterResumeTime_ContinuesChain()
    {
        var delayNode = new DelayNode(Guid.NewGuid(), 60);
        var nextNode = new TestNode(
            Guid.NewGuid(),
            "next",
            false,
            context =>
            {
                context.Session.Variables["next"] = "visited";
                return Task.FromResult(NodeResult.Continue());
            });
        var graph = ScenarioGraphFactory.CreateLinear(delayNode, nextNode);
        var session = new TestSession
        {
            CurrentNodeId = delayNode.NodeId,
            State = SessionState.WaitingForEvent,
        };
        session.Variables["__delay_resume_at"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("O");

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "resume" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.State).IsEqualTo(SessionState.Completed);
        await Assert.That(session.CurrentNodeId).IsNull();
        await Assert.That(session.Variables.ContainsKey("__delay_resume_at")).IsFalse();
        await Assert.That(session.Variables["next"]).IsEqualTo("visited");
    }

    [Test]
    public async Task ExecuteAsync_AwaitingNode_Error_ReturnsError()
    {
        var awaitingNode = new TestNode(
            Guid.NewGuid(),
            "recv",
            true,
            _ => Task.FromResult(NodeResult.Error("ошибка ожидания")));
        var graph = ScenarioGraphFactory.CreateLinear(awaitingNode);
        var session = new TestSession
        {
            CurrentNodeId = awaitingNode.NodeId,
            State = SessionState.WaitingForEvent,
        };

        var result = await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Text = "hello" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors![0]).IsEqualTo("ошибка ожидания");
    }

    [Test]
    public async Task ExecuteAsync_PayloadMatchesEntryPoint_NavigatesToEntryNode()
    {
        var entryNode = new TestNode(
            Guid.NewGuid(),
            "entry",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "entry";
                return Task.FromResult(NodeResult.Continue());
            });
        var startNode = new TestNode(
            Guid.NewGuid(),
            "start",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "start";
                return Task.FromResult(NodeResult.Continue());
            });

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["menu"] = entryNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            startNode.NodeId,
            new Dictionary<Guid, INode>
            {
                [startNode.NodeId] = startNode,
                [entryNode.NodeId] = entryNode,
            },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var session = new TestSession();

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Payload = "menu" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["visited"]).IsEqualTo("entry");
    }

    [Test]
    public async Task ExecuteAsync_PayloadMatchesEntryPoint_ClearsVariablesBeforeNavigation()
    {
        var entryNode = new TestNode(
            Guid.NewGuid(),
            "entry",
            false,
            _ => Task.FromResult(NodeResult.Continue()));

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["menu"] = entryNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            entryNode.NodeId,
            new Dictionary<Guid, INode> { [entryNode.NodeId] = entryNode },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var session = new TestSession();
        session.Variables["stale"] = "value";

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Payload = "menu" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables.ContainsKey("stale")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_NoPayload_StartsFromStartNode()
    {
        var startNode = new TestNode(
            Guid.NewGuid(),
            "start",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "start";
                return Task.FromResult(NodeResult.Continue());
            });
        var entryNode = new TestNode(
            Guid.NewGuid(),
            "entry",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "entry";
                return Task.FromResult(NodeResult.Continue());
            });

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["menu"] = entryNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            startNode.NodeId,
            new Dictionary<Guid, INode>
            {
                [startNode.NodeId] = startNode,
                [entryNode.NodeId] = entryNode,
            },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var session = new TestSession();

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent(),
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["visited"]).IsEqualTo("start");
    }

    [Test]
    public async Task ExecuteAsync_PayloadNotInEntryPoints_FallsBackToStartNode()
    {
        var startNode = new TestNode(
            Guid.NewGuid(),
            "start",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "start";
                return Task.FromResult(NodeResult.Continue());
            });
        var entryNode = new TestNode(
            Guid.NewGuid(),
            "entry",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "entry";
                return Task.FromResult(NodeResult.Continue());
            });

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["menu"] = entryNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            startNode.NodeId,
            new Dictionary<Guid, INode>
            {
                [startNode.NodeId] = startNode,
                [entryNode.NodeId] = entryNode,
            },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var session = new TestSession();

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Payload = "unknown" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["visited"]).IsEqualTo("start");
    }

    [Test]
    public async Task ExecuteAsync_EntryNodeContinuesChain_AfterNavigation()
    {
        var entryNode = new TestNode(
            Guid.NewGuid(),
            "entry",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var nextNode = new TestNode(
            Guid.NewGuid(),
            "next",
            false,
            ctx =>
            {
                ctx.Session.Variables["reached"] = "next";
                return Task.FromResult(NodeResult.Continue());
            });

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["menu"] = entryNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            entryNode.NodeId,
            new Dictionary<Guid, INode>
            {
                [entryNode.NodeId] = entryNode,
                [nextNode.NodeId] = nextNode,
            },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>
            {
                [entryNode.NodeId] =
                [
                    new NodeConnection(entryNode.NodeId, nextNode.NodeId, "default"),
                ],
            },
            entryPoints);
        var session = new TestSession();

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Payload = "menu" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["reached"]).IsEqualTo("next");
    }

    [Test]
    public async Task ExecuteAsync_WaitingOnEntryNode_Reroutes_ByNewPayload()
    {
        var useCase = Substitute.For<IReceiveButtonPressUseCase>();
        useCase.CanHandle(Arg.Any<IIncomingEvent>()).Returns(true);
        useCase.ExtractPayload(Arg.Any<IIncomingEvent>()).Returns("help");

        var entryButtonNode = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            useCase,
            ["menu"],
            isEntry: true);
        var helpNode = new TestNode(
            Guid.NewGuid(),
            "help",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "help";
                return Task.FromResult(NodeResult.Continue());
            });

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["menu"] = entryButtonNode.NodeId,
            ["help"] = helpNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            entryButtonNode.NodeId,
            new Dictionary<Guid, INode>
            {
                [entryButtonNode.NodeId] = entryButtonNode,
                [helpNode.NodeId] = helpNode,
            },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var session = new TestSession
        {
            CurrentNodeId = entryButtonNode.NodeId,
            State = SessionState.WaitingForEvent,
        };

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Payload = "help" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["visited"]).IsEqualTo("help");
    }

    [Test]
    public async Task ExecuteAsync_WaitingOnNonEntryButton_WaitResult_RoutesToEntry()
    {
        var currentUseCase = Substitute.For<IReceiveButtonPressUseCase>();
        currentUseCase.CanHandle(Arg.Any<IIncomingEvent>()).Returns(true);
        currentUseCase.ExtractPayload(Arg.Any<IIncomingEvent>()).Returns("unknown");

        var currentButtonNode = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            currentUseCase,
            ["yes", "no"],
            isEntry: false);
        var entryTargetNode = new TestNode(
            Guid.NewGuid(),
            "menu",
            false,
            ctx =>
            {
                ctx.Session.Variables["visited"] = "menu";
                return Task.FromResult(NodeResult.Continue());
            });

        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["unknown"] = entryTargetNode.NodeId,
        };
        var graph = ScenarioGraphFactory.Create(
            currentButtonNode.NodeId,
            new Dictionary<Guid, INode>
            {
                [currentButtonNode.NodeId] = currentButtonNode,
                [entryTargetNode.NodeId] = entryTargetNode,
            },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var session = new TestSession
        {
            CurrentNodeId = currentButtonNode.NodeId,
            State = SessionState.WaitingForEvent,
        };

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { Payload = "unknown" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables["visited"]).IsEqualTo("menu");
    }

    [Test]
    public async Task ExecuteAsync_FreshStart_ClearsSessionVariables()
    {
        var startNode = new TestNode(
            Guid.NewGuid(),
            "start",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var graph = ScenarioGraphFactory.CreateLinear(startNode);
        var session = new TestSession();
        session.Variables["leftover"] = "value";

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent(),
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables.ContainsKey("leftover")).IsFalse();
    }

    [Test]
    public async Task ExecuteAsync_StartFlow_PlacesSystemVariablesInSession()
    {
        var startNode = new TestNode(
            Guid.NewGuid(),
            "start",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var graph = ScenarioGraphFactory.CreateLinear(startNode);
        var botId = Guid.NewGuid();
        var session = new TestSession { BotId = botId };

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { ChatId = "chat_42", PlatformUserId = "user_99" },
            session,
            Guid.NewGuid(),
            7,
            string.Empty);

        await Assert.That(session.Variables["_chatId"]).IsEqualTo("chat_42");
        await Assert.That(session.Variables["_userId"]).IsEqualTo("user_99");
        await Assert.That(session.Variables["_botId"]).IsEqualTo(botId.ToString());
        await Assert.That(session.Variables["_scenarioVersion"]).IsEqualTo("7");
    }

    [Test]
    public async Task ExecuteAsync_AfterEntryPointClear_SystemVariablesAreReapplied()
    {
        var receiveUseCase = Substitute.For<IReceiveButtonPressUseCase>();
        receiveUseCase.CanHandle(Arg.Any<IIncomingEvent>()).Returns(true);
        receiveUseCase.ExtractPayload(Arg.Any<IIncomingEvent>()).Returns("menu");

        var entryNode = new ReceiveButtonPressNode(
            Guid.NewGuid(),
            "btn",
            receiveUseCase,
            ["menu"],
            isEntry: true);
        var entryPoints = new Dictionary<string, Guid>(StringComparer.Ordinal) { ["menu"] = entryNode.NodeId };
        var graph = ScenarioGraphFactory.Create(
            entryNode.NodeId,
            new Dictionary<Guid, INode> { [entryNode.NodeId] = entryNode },
            new Dictionary<Guid, IReadOnlyList<NodeConnection>>(),
            entryPoints);
        var botId = Guid.NewGuid();
        var session = new TestSession { BotId = botId };
        session.Variables["userField"] = "to_be_cleared";

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { ChatId = "chat_77", PlatformUserId = "user_55", Payload = "menu" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables.ContainsKey("userField")).IsFalse();
        await Assert.That(session.Variables["_chatId"]).IsEqualTo("chat_77");
        await Assert.That(session.Variables["_userId"]).IsEqualTo("user_55");
        await Assert.That(session.Variables["_botId"]).IsEqualTo(botId.ToString());
    }

    [Test]
    public async Task ExecuteAsync_AfterStartRouteClear_SystemVariablesAreReapplied()
    {
        var startNode = new TestNode(
            Guid.NewGuid(),
            "start",
            false,
            _ => Task.FromResult(NodeResult.Continue()));
        var graph = ScenarioGraphFactory.CreateLinear(startNode);
        var session = new TestSession();
        session.Variables["userField"] = "to_be_cleared";

        await CreateExecutor().ExecuteAsync(
            graph,
            new TestIncomingEvent { ChatId = "chat_X" },
            session,
            Guid.NewGuid(),
            string.Empty);

        await Assert.That(session.Variables.ContainsKey("userField")).IsFalse();
        await Assert.That(session.Variables["_chatId"]).IsEqualTo("chat_X");
    }

    private static ScenarioExecutor CreateExecutor() =>
        new(Substitute.For<IDataStore>(), Substitute.For<ISchemaStore>());
}
