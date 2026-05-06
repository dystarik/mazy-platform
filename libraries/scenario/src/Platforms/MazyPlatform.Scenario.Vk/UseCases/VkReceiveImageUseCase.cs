namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// VK-реализация приёма изображения.
/// </summary>
public sealed class VkReceiveImageUseCase : IReceiveImageUseCase
{
    /// <inheritdoc />
    public bool CanHandle(IIncomingEvent incomingEvent) => incomingEvent.EventType == IncomingEventType.Image;

    /// <inheritdoc />
    public string ExtractImageUrl(IIncomingEvent incomingEvent) => incomingEvent.ImageUrl ?? string.Empty;

    /// <inheritdoc />
    public string? ExtractCaption(IIncomingEvent incomingEvent) => incomingEvent.ImageCaption;
}
