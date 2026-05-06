namespace MazyPlatform.Scenario.Nodes.Messages;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Abstractions.Validation;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел получения текстового сообщения от пользователя.
/// Ожидает входящее событие, опционально валидирует ввод.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла получения сообщения.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="messageTextVariable">Имя переменной для сохранения текста сообщения.</param>
/// <param name="receiveMessageUseCase">Юзкейс получения сообщения.</param>
/// <param name="sendMessageUseCase">Юзкейс отправки (для сообщений об ошибке валидации).</param>
/// <param name="validators">Зарегистрированные валидаторы ввода.</param>
/// <param name="validatorType">Тип валидации (опционально).</param>
/// <param name="validationParams">Параметры валидации (опционально).</param>
/// <param name="errorMessage">Сообщение при невалидном вводе (опционально).</param>
/// <param name="messageIdVariable">Имя переменной для сохранения ID входящего сообщения (опционально).</param>
public sealed class ReceiveMessageNode(
    Guid nodeId,
    string messageTextVariable,
    IReceiveMessageUseCase receiveMessageUseCase,
    ISendMessageUseCase sendMessageUseCase,
    IReadOnlyDictionary<string, IInputValidator> validators,
    string? validatorType = null,
    IReadOnlyDictionary<string, string>? validationParams = null,
    string? errorMessage = null,
    string? messageIdVariable = null) : NodeBase(nodeId, "receive_message", isAwaiting: true)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.IncomingEvent is null || !receiveMessageUseCase.CanHandle(context.IncomingEvent))
            return NodeResult.Wait();

        var text = receiveMessageUseCase.ExtractText(context.IncomingEvent);

        if (validatorType is not null
            && validators.TryGetValue(validatorType, out var validator)
            && !validator.IsValid(text, validationParams))
        {
            var message = errorMessage ?? "Неверный формат ввода. Попробуйте ещё раз.";
            var sendR = await sendMessageUseCase.ExecuteAsync(context, message, cancellationToken);
            return NodeResult.Wait(sendR.Action);
        }

        context.Session.Variables[messageTextVariable] = text;
        if (messageIdVariable is not null && !string.IsNullOrEmpty(context.IncomingEvent.MessageId))
        {
            context.Session.Variables[messageIdVariable] = context.IncomingEvent.MessageId;
        }

        return NodeResult.Continue();
    }
}
