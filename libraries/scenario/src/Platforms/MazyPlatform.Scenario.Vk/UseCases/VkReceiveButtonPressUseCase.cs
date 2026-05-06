namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Text.Json;

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
    public string ExtractPayload(IIncomingEvent incomingEvent)
    {
        var raw = incomingEvent.Payload ?? string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(raw);

            if (doc.RootElement.TryGetProperty("p", out var p))
                return p.GetString() ?? raw;
        }
        catch (JsonException)
        {
        }

        return raw;
    }
}
