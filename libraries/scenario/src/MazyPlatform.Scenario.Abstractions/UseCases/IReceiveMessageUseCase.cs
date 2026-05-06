namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Проверяет, является ли входящее событие сообщением,
/// и извлекает из него данные.
/// </summary>
public interface IReceiveMessageUseCase
{
    /// <summary>
    /// Проверяет, подходит ли событие.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns>true, если событие — сообщение, которое узел может принять.</returns>
    bool CanHandle(IIncomingEvent incomingEvent);

    /// <summary>
    /// Извлекает текст сообщения из события.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns>Текст сообщения.</returns>
    string ExtractText(IIncomingEvent incomingEvent);
}
