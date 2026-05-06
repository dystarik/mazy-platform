namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: удалить ранее отправленное сообщение.
/// </summary>
/// <param name="MessageId">Идентификатор сообщения на платформе.</param>
public sealed record DeleteMessageAction(string MessageId) : IOutgoingAction;
