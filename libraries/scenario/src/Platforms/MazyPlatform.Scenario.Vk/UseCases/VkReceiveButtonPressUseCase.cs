namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// VK-реализация приёма нажатия кнопки.
/// </summary>
public sealed class VkReceiveButtonPressUseCase : IReceiveButtonPressUseCase
{
    /// <inheritdoc />
    public bool CanHandle(IIncomingEvent incomingEvent) => incomingEvent.EventType == IncomingEventType.ButtonPress;

    /// <inheritdoc />
    public string ExtractPayload(IIncomingEvent incomingEvent) => incomingEvent.Payload ?? string.Empty;
}
