namespace MazyPlatform.Scenario.Abstractions.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;

/// <summary>
/// Проверяет, является ли входящее событие нажатием кнопки,
/// и извлекает payload.
/// </summary>
public interface IReceiveButtonPressUseCase
{
    /// <summary>
    /// Проверяет, может ли обработчик обработать указанное входящее событие.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns><see langword="true"/>, если событие является нажатием кнопки; иначе <see langword="false"/>.</returns>
    bool CanHandle(IIncomingEvent incomingEvent);

    /// <summary>
    /// Извлекает payload из входящего события нажатия кнопки.
    /// </summary>
    /// <param name="incomingEvent">Входящее событие.</param>
    /// <returns>Строковый payload нажатой кнопки.</returns>
    string ExtractPayload(IIncomingEvent incomingEvent);
}
