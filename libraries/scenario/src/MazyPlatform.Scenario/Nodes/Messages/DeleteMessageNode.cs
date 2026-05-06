namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел удаления ранее отправленного сообщения.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла удаления сообщения.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="messageIdVariable">Имя переменной сессии с идентификатором сообщения.</param>
/// <param name="deleteMessageUseCase">Юзкейс удаления.</param>
public sealed class DeleteMessageNode(
    Guid nodeId,
    string messageIdVariable,
    IDeleteMessageUseCase deleteMessageUseCase) : NodeBase(nodeId, "delete_message", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Session.Variables.TryGetValue(messageIdVariable, out var messageIdObj)
            || messageIdObj is not string messageId
            || string.IsNullOrEmpty(messageId))
        {
            // Переменная не задана или пустая — пропускаем.
            // Это логирование может быть на уровне исполнителя; здесь — тихо продолжаем.
            return NodeResult.Continue();
        }

        var action = await deleteMessageUseCase.ExecuteAsync(context, messageId, cancellationToken);
        return NodeResult.Continue(action);
    }
}
