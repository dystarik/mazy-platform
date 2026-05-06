namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: отправить сообщение с кнопками.
/// </summary>
/// <param name="Text">Текст сообщения.</param>
/// <param name="Buttons">Кнопки для отображения.</param>
public sealed record SendButtonsAction(
    string Text,
    ButtonLayout Buttons) : IOutgoingAction;
