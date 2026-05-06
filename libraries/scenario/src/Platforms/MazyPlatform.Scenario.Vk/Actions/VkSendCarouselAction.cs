namespace MazyPlatform.Scenario.Vk.Actions;

using MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Действие: отправить карусель карточек VK.
/// </summary>
/// <param name="TemplateJson">JSON карусели в формате VK.</param>
public sealed record VkSendCarouselAction(string TemplateJson) : IOutgoingAction;
