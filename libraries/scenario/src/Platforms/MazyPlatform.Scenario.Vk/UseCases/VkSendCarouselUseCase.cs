namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk;
using MazyPlatform.Scenario.Vk.Actions;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// Юзкейс отправки карусели карточек VK.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkSendCarouselUseCase(VkApiClient apiClient)
{
    /// <summary>
    /// Отправляет карусель карточек.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="cards">Карточки карусели.</param>
    /// <param name="text">Текст сообщения, отправляемого вместе с каруселью. Обязательный (требование VK API).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отправки карусели с действием и опциональным идентификатором сообщения.</returns>
    public async Task<SendResult> ExecuteAsync(ExecutionContext context, IReadOnlyList<VkCarouselCard> cards, string text, CancellationToken cancellationToken = default)
    {
        var templateJson = BuildTemplateJson(cards);

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["random_id"] = Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["message"] = text,
            ["template"] = templateJson,
        };

        var response = await apiClient.CallAsync("messages.send", parameters, context.BotToken, cancellationToken);
        var messageId = VkMessageId.FromSendResponse(response);

        return new SendResult(new VkSendCarouselAction(templateJson), messageId);
    }

    private static string BuildTemplateJson(IReadOnlyList<VkCarouselCard> cards)
    {
        return JsonSerializer.Serialize(new
        {
            type = "carousel",
            elements = cards.Select(BuildCardElement),
        });
    }

    private static object BuildCardElement(VkCarouselCard card)
    {
        var buttons = new List<object>(card.Buttons.Count);

        buttons.AddRange(card.Buttons.Select(BuildCardButton));

        return new
        {
            title = card.Title,
            description = card.Description,
            photo_id = card.PhotoId,
            buttons,
        };
    }

    private static object BuildCardButton(VkCarouselButton button)
    {
        if (button.Link is not null)
        {
            return new
            {
                action = new
                {
                    type = "open_link",
                    label = button.Label,
                    payload = VkButtonPayloadFormatter.Format(button.Payload),
                    link = button.Link,
                },
            };
        }

        return new
        {
            action = new
            {
                type = "callback",
                label = button.Label,
                payload = VkButtonPayloadFormatter.Format(button.Payload),
            },
        };
    }
}
