namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: отправить текстовое сообщение пользователю.
/// </summary>
/// <param name="Text">Текст сообщения (с уже подставленными переменными).</param>
public sealed record SendTextAction(string Text) : IOutgoingAction;
