namespace MazyPlatform.Scenario.Vk.Helpers;

using System.Text.Json;

using MazyPlatform.Scenario.Vk.UseCases;

/// <summary>
/// Вспомогательные методы для извлечения параметров VK-узлов из JSON.
/// </summary>
public static class VkJsonParamHelper
{
    /// <summary>
    /// Парсит строки кнопок VK-клавиатуры из параметров узла.
    /// </summary>
    /// <param name="parameters">JSON параметров узла.</param>
    /// <returns>Список строк кнопок.</returns>
    public static IReadOnlyList<IReadOnlyList<VkButtonInfo>> ParseKeyboardButtons(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("buttons", out var buttonsElement) || buttonsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<List<VkButtonInfo>>();

        foreach (var row in buttonsElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array)
                continue;

            var buttonRow = new List<VkButtonInfo>();

            foreach (var button in row.EnumerateArray())
            {
                buttonRow.Add(new VkButtonInfo(
                    button.GetProperty("label").GetString() ?? string.Empty,
                    button.GetProperty("payload").GetString() ?? string.Empty,
                    button.TryGetProperty("color", out var colorProp) ? colorProp.GetString() : null));
            }

            rows.Add(buttonRow);
        }

        return rows;
    }

    /// <summary>
    /// Парсит карточки VK-карусели из параметров узла.
    /// </summary>
    /// <param name="parameters">JSON параметров узла.</param>
    /// <returns>Список карточек карусели.</returns>
    public static IReadOnlyList<VkCarouselCard> ParseCarouselCards(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("cards", out var cardsElement) ||
            cardsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return cardsElement
            .EnumerateArray()
            .Select(ParseSingleCard)
            .ToList();
    }

    /// <summary>
    /// Парсит одну карточку VK-карусели.
    /// </summary>
    /// <param name="card">JSON карточки.</param>
    /// <returns>Карточка карусели.</returns>
    public static VkCarouselCard ParseSingleCard(JsonElement card)
    {
        var title = card.GetProperty("title").GetString() ?? string.Empty;
        var description = card.GetProperty("description").GetString() ?? string.Empty;
        var photoId = card.TryGetProperty("photoId", out var photoProp) ? photoProp.GetString() : null;

        var buttons = new List<VkCarouselButton>();

        if (card.TryGetProperty("buttons", out var buttonsElement) && buttonsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var button in buttonsElement.EnumerateArray())
            {
                buttons.Add(new VkCarouselButton(
                    button.GetProperty("label").GetString() ?? string.Empty,
                    button.GetProperty("payload").GetString() ?? string.Empty,
                    button.TryGetProperty("link", out var linkProp) ? linkProp.GetString() : null));
            }
        }

        return new VkCarouselCard(title, description, photoId, buttons);
    }
}
