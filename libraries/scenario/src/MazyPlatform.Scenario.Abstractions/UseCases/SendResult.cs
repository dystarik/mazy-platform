namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Результат отправки сообщения через платформенный юзкейс.
/// </summary>
/// <param name="Action">Сформированное исходящее действие.</param>
/// <param name="MessageId">
/// Идентификатор отправленного сообщения на платформе.
/// Может быть null, если платформа не вернула числовой ID в ответе.
/// </param>
public sealed record SendResult(IOutgoingAction Action, string? MessageId);
