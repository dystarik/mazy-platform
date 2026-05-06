namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел ожидания нажатия кнопки.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла ожидания нажатия кнопки.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="buttonPayloadVariable">Имя переменной для сохранения payload нажатой кнопки.</param>
/// <param name="receiveButtonPressUseCase">Юзкейс получения нажатия кнопки.</param>
/// <param name="expectedPayloads">Допустимые payload (опционально).</param>
/// <param name="isEntry">Признак точки входа (entry point).</param>
public sealed class ReceiveButtonPressNode(
    Guid nodeId,
    string buttonPayloadVariable,
    IReceiveButtonPressUseCase receiveButtonPressUseCase,
    IReadOnlyList<string>? expectedPayloads = null,
    bool isEntry = false) : NodeBase(nodeId, "receive_button_press", isAwaiting: true)
{
    /// <summary>
    /// Признак того, что данный узел является точкой входа в сценарий
    /// по нажатию кнопки.
    /// </summary>
    /// <remarks>
    /// Если <c>true</c>, узел регистрируется в <c>ScenarioGraph.EntryPoints</c>
    /// при сборке графа (см. <c>ScenarioBuilder.BuildEntryPoints</c>) — по
    /// одной записи на каждый payload из <see cref="ExpectedPayloads"/>.
    /// <para>
    /// <b>Сайд-эффект на жизненный цикл сессии</b>: при поступлении входящего
    /// события с payload, совпадающим с одной из записей <c>EntryPoints</c>,
    /// исполнитель (<c>ScenarioExecutor.NavigateToEntryOrStart</c>) перебивает
    /// текущее состояние сессии — даже если она в этот момент в
    /// <c>WaitingForEvent</c> на другом узле, — <b>очищает все переменные
    /// сессии</b> (<c>Session.Variables.Clear()</c>) и стартует выполнение
    /// с этого entry-узла. Это сделано для того, чтобы entry-кнопки работали
    /// как «глобальные» точки входа в любой момент диалога (например, кнопка
    /// «Главное меню» сбрасывает текущий процесс и начинает новый).
    /// </para>
    /// <para>
    /// Из этого следует: переменные, накопленные до нажатия entry-кнопки,
    /// в новом проходе уже недоступны. Это намеренно — entry-маршрут стартует
    /// с чистого листа.
    /// </para>
    /// </remarks>
    public bool IsEntry { get; } = isEntry;

    /// <summary>
    /// Допустимые payload для данного узла.
    /// </summary>
    public IReadOnlyList<string>? ExpectedPayloads { get; } = expectedPayloads;

    /// <inheritdoc />
    public override Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.IncomingEvent is null || !receiveButtonPressUseCase.CanHandle(context.IncomingEvent))
            return Task.FromResult(NodeResult.Wait());

        var payload = receiveButtonPressUseCase.ExtractPayload(context.IncomingEvent);

        if (ExpectedPayloads is { Count: > 0 } && !ExpectedPayloads.Contains(payload, StringComparer.Ordinal))
            return Task.FromResult(NodeResult.Wait());

        context.Session.Variables[buttonPayloadVariable] = payload;
        return Task.FromResult(NodeResult.Continue());
    }
}
