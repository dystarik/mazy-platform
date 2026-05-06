namespace MazyPlatform.Scenario.Executor;

using System.Globalization;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Abstractions.Sessions;
using MazyPlatform.Scenario.Nodes.Messages;

/// <summary>
/// Движок выполнения сценария.
/// Реактивная модель: событие → цикл выполнения узлов → действия.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр движка выполнения.
/// </remarks>
/// <param name="dataStore">Хранилище пользовательских данных.</param>
/// <param name="schemaStore">Хранилище схем сущностей.</param>
public sealed class ScenarioExecutor(IDataStore dataStore, ISchemaStore schemaStore) : IScenarioExecutor
{
    private const int MaxIterations = 1000;

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteAsync(
        ScenarioGraph graph,
        IIncomingEvent incomingEvent,
        ISession session,
        Guid projectId,
        string botToken,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            graph,
            incomingEvent,
            session,
            projectId,
            scenarioVersion: 0,
            botToken: botToken,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteAsync(
        ScenarioGraph graph,
        IIncomingEvent incomingEvent,
        ISession session,
        Guid projectId,
        int scenarioVersion,
        string botToken,
        CancellationToken cancellationToken = default)
    {
        var previousState = session.State;
        var context = new ExecutionContext
        {
            Session = session,
            IncomingEvent = incomingEvent,
            DataStore = dataStore,
            SchemaStore = schemaStore,
            ProjectId = projectId,
            ScenarioVersion = scenarioVersion,
            BotToken = botToken,
        };
        var actions = new List<IOutgoingAction>();

        session.State = SessionState.Active;
        ApplySystemVariables(context);

        if (previousState == SessionState.WaitingForEvent && session.CurrentNodeId is not null)
            return await HandleWaitingState(graph, context, actions, cancellationToken);

        return await NavigateToEntryOrStart(graph, context, actions, cancellationToken);
    }

    /// <summary>
    /// Устанавливает в Session.Variables системные переменные с префиксом "_",
    /// доступные во всех шаблонах и параметрах узлов.
    /// </summary>
    /// <remarks>
    /// Вызывается в начале каждого выполнения сценария и после каждого
    /// <c>Session.Variables.Clear()</c> в <see cref="NavigateToEntryOrStart"/> —
    /// иначе очистка переменных при переходе на entry-маршрут стирала бы и
    /// системные ключи, и шаблоны вида <c>{_chatId}</c> в первом узле после
    /// перехода переставали бы резолвиться.
    /// </remarks>
    private static void ApplySystemVariables(ExecutionContext context)
    {
        context.Session.Variables["_chatId"] = context.IncomingEvent?.ChatId ?? string.Empty;
        context.Session.Variables["_userId"] = context.IncomingEvent?.PlatformUserId ?? string.Empty;
        context.Session.Variables["_botId"] = context.Session.BotId.ToString();
        context.Session.Variables["_scenarioVersion"] = context.ScenarioVersion.ToString(CultureInfo.InvariantCulture);
    }

    private static async Task<ExecutionResult> HandleWaitingState(
        ScenarioGraph graph,
        ExecutionContext context,
        List<IOutgoingAction> actions,
        CancellationToken cancellationToken)
    {
        var session = context.Session;
        var currentNodeId = session.CurrentNodeId!.Value;

        if (!graph.Nodes.TryGetValue(currentNodeId, out var currentNode))
            return CreateErrorResult(session, actions, $"Узел {currentNodeId} не найден в графе.");

        // Если входящий payload совпадает с точкой входа — перезапускаем сценарий,
        // независимо от того на каком узле остановилась сессия.
        var incomingPayload = context.IncomingEvent?.Payload;

        if (incomingPayload is not null && graph.EntryPoints.ContainsKey(incomingPayload))
            return await NavigateToEntryOrStart(graph, context, actions, cancellationToken);

        // Сессия остановилась на самом entry-узле и пришло любое событие —
        // entry-узел выполняется заново (с очисткой переменных).
        if (currentNode is ReceiveButtonPressNode { IsEntry: true })
            return await NavigateToEntryOrStart(graph, context, actions, cancellationToken);

        var result = await currentNode.ExecuteAsync(context, cancellationToken);

        if (result.Status == NodeExecutionStatus.Error)
            return CreateErrorResult(session, actions, result.ErrorMessage);

        if (result.Status == NodeExecutionStatus.WaitForEvent)
        {
            // Не-entry receive-узел не нашёл ожидаемого события — пробуем
            // переключиться на entry-маршрут или начать сценарий с старта.
            if (currentNode is ReceiveButtonPressNode { IsEntry: false })
                return await NavigateToEntryOrStart(graph, context, actions, cancellationToken);

            session.CurrentNodeId = currentNodeId;
            session.State = SessionState.WaitingForEvent;
            CollectActions(actions, result);
            return new ExecutionResult(session, actions, SessionState.WaitingForEvent);
        }

        CollectActions(actions, result);
        var nextNodeId = ResolveNextNode(graph, currentNodeId, result, session, out var finished);

        if (finished)
            return new ExecutionResult(session, actions, session.State);

        return await RunChain(graph, context, actions, nextNodeId, cancellationToken);
    }

    /// <summary>
    /// Перенаправляет выполнение на entry-узел по payload входящего события
    /// или на стартовый узел сценария, если совпадения нет.
    /// </summary>
    /// <remarks>
    /// <b>Сайд-эффект</b>: в обоих ветках перед запуском нового узла
    /// <c>Session.Variables.Clear()</c> — переменные, накопленные предыдущим
    /// проходом сценария, удаляются. Это нужно для того, чтобы entry-кнопки
    /// и старт сценария всегда работали «с чистого листа», независимо от
    /// текущего состояния сессии.
    /// <para>
    /// Метод вызывается из <see cref="HandleWaitingState"/>, когда:
    /// (1) пришёл payload, совпадающий с зарегистрированной точкой входа,
    /// (2) сессия стоит на entry-узле и получила любое событие,
    /// (3) не-entry receive-узел не нашёл ожидаемого события (fallback).
    /// </para>
    /// </remarks>
    private static async Task<ExecutionResult> NavigateToEntryOrStart(
        ScenarioGraph graph,
        ExecutionContext context,
        List<IOutgoingAction> actions,
        CancellationToken cancellationToken)
    {
        var session = context.Session;
        var payload = context.IncomingEvent?.Payload;

        if (payload is not null && graph.EntryPoints.TryGetValue(payload, out var entryNodeId))
        {
            session.Variables.Clear();
            ApplySystemVariables(context);

            if (!graph.Nodes.TryGetValue(entryNodeId, out var entryNode))
                return CreateErrorResult(session, actions, $"Узел {entryNodeId} не найден в графе.");

            var result = await entryNode.ExecuteAsync(context, cancellationToken);

            if (result.Status == NodeExecutionStatus.Error)
                return CreateErrorResult(session, actions, result.ErrorMessage);

            CollectActions(actions, result);
            var nextNodeId = ResolveNextNode(graph, entryNodeId, result, session, out var finished);

            if (finished)
                return new ExecutionResult(session, actions, session.State);

            return await RunChain(graph, context, actions, nextNodeId, cancellationToken);
        }

        session.Variables.Clear();
        ApplySystemVariables(context);
        return await RunChain(graph, context, actions, graph.StartNodeId, cancellationToken);
    }

    private static async Task<ExecutionResult> RunChain(
        ScenarioGraph graph,
        ExecutionContext context,
        List<IOutgoingAction> actions,
        Guid startNodeId,
        CancellationToken cancellationToken)
    {
        var session = context.Session;
        var currentNodeId = startNodeId;
        var iterations = 0;

        while (iterations++ < MaxIterations)
        {
            if (!graph.Nodes.TryGetValue(currentNodeId, out var currentNode))
                return CreateErrorResult(session, actions, $"Узел {currentNodeId} не найден в графе.");

            if (currentNode.IsAwaiting)
            {
                session.CurrentNodeId = currentNodeId;
                session.State = SessionState.WaitingForEvent;
                return new ExecutionResult(session, actions, SessionState.WaitingForEvent);
            }

            var result = await currentNode.ExecuteAsync(context, cancellationToken);

            if (result.Status == NodeExecutionStatus.Error)
                return CreateErrorResult(session, actions, result.ErrorMessage);

            CollectActions(actions, result);

            if (result.Status == NodeExecutionStatus.WaitForEvent)
            {
                session.CurrentNodeId = currentNodeId;
                session.State = SessionState.WaitingForEvent;
                return new ExecutionResult(session, actions, SessionState.WaitingForEvent);
            }

            currentNodeId = ResolveNextNode(graph, currentNodeId, result, session, out var finished);

            if (finished)
                return new ExecutionResult(session, actions, session.State);
        }

        return CreateErrorResult(session, actions, "Превышено максимальное количество итераций.");
    }

    private static Guid ResolveNextNode(
        ScenarioGraph graph,
        Guid currentNodeId,
        NodeResult result,
        ISession session,
        out bool finished)
    {
        var nextNodeId = graph.GetNextNodeId(currentNodeId, result.BranchKey);

        if (nextNodeId is null)
        {
            session.CurrentNodeId = null;
            session.State = SessionState.Completed;
            finished = true;
            return Guid.Empty;
        }

        finished = false;
        return nextNodeId.Value;
    }

    private static void CollectActions(List<IOutgoingAction> actions, NodeResult result)
    {
        if (result.Actions is { Count: > 0 })
            actions.AddRange(result.Actions);
    }

    private static ExecutionResult CreateErrorResult(ISession session, List<IOutgoingAction> actions, string? errorMessage)
    {
        session.State = SessionState.Completed;
        return new ExecutionResult(session, actions, SessionState.Completed, [errorMessage ?? "Неизвестная ошибка"]);
    }
}
