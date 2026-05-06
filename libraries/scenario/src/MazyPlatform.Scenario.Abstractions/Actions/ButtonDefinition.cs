namespace MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Описание кнопки.
/// </summary>
/// <param name="Label">Текст на кнопке.</param>
/// <param name="Payload">Данные, отправляемые при нажатии.</param>
/// <param name="Style">Семантический стиль кнопки (опционально).</param>
public sealed record ButtonDefinition(string Label, string Payload, ButtonStyle? Style = null);
