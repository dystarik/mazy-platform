namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// VK-реализация приёма текстового сообщения.
/// </summary>
public sealed class VkReceiveMessageUseCase : IReceiveMessageUseCase
{
    /// <inheritdoc />
    public bool CanHandle(IIncomingEvent incomingEvent) => incomingEvent.EventType == IncomingEventType.Message;

    /// <inheritdoc />
    public string ExtractText(IIncomingEvent incomingEvent) => incomingEvent.Text ?? string.Empty;
}
