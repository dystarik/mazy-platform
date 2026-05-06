namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: редактировать ранее отправленное сообщение.
/// </summary>
/// <param name="MessageId">Идентификатор сообщения на платформе.</param>
/// <param name="NewText">Новый текст сообщения.</param>
/// <param name="Buttons">Новый набор кнопок. Если <see langword="null"/> — кнопки не отправляются.</param>
public sealed record EditMessageAction(
    string MessageId,
    string NewText,
    ButtonLayout? Buttons) : IOutgoingAction;
