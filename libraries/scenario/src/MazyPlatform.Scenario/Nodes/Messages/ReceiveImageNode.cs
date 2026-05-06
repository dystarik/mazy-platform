namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел получения изображения от пользователя.
/// Сохраняет URL изображения и подпись в переменные сессии.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла получения изображения.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="imageUrlVariable">Переменная для сохранения URL изображения.</param>
/// <param name="receiveImageUseCase">Юзкейс получения изображения.</param>
/// <param name="captionVariable">Переменная для сохранения подписи (опционально).</param>
public sealed class ReceiveImageNode(
    Guid nodeId,
    string imageUrlVariable,
    IReceiveImageUseCase receiveImageUseCase,
    string? captionVariable = null) : NodeBase(nodeId, "receive_image", isAwaiting: true)
{
    /// <inheritdoc />
    public override Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context.IncomingEvent is null || !receiveImageUseCase.CanHandle(context.IncomingEvent))
            return Task.FromResult(NodeResult.Wait());

        context.Session.Variables[imageUrlVariable] = receiveImageUseCase.ExtractImageUrl(context.IncomingEvent);

        if (captionVariable is not null)
        {
            context.Session.Variables[captionVariable] = receiveImageUseCase.ExtractCaption(context.IncomingEvent);
        }

        return Task.FromResult(NodeResult.Continue());
    }
}
