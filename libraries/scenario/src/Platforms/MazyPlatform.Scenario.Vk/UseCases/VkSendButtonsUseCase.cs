namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация отправки сообщения с кнопками.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkSendButtonsUseCase(VkApiClient apiClient) : ISendButtonsUseCase
{
    /// <inheritdoc />
    public async Task<SendResult> ExecuteAsync(
        ExecutionContext context,
        string text,
        ButtonLayout buttons,
        CancellationToken cancellationToken = default)
    {
        var keyboardJson = BuildKeyboardJson(buttons);

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["random_id"] = Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["message"] = text,
            ["keyboard"] = keyboardJson,
        };

        var response = await apiClient.CallAsync("messages.send", parameters, context.BotToken, cancellationToken);
        var messageId = response.ValueKind == JsonValueKind.Number ? response.ToString() : null;

        return new SendResult(new SendButtonsAction(text, buttons), messageId);
    }

    private static string BuildKeyboardJson(ButtonLayout buttons)
    {
        var rows = new List<List<object>>(buttons.Rows.Count);

        foreach (var row in buttons.Rows)
        {
            var vkRow = new List<object>(row.Count);

            foreach (var button in row)
            {
                vkRow.Add(new
                {
                    action = new
                    {
                        type = "callback",
                        label = button.Label,
                        payload = button.Payload,
                    },
                    color = MapStyle(button.Style),
                });
            }

            rows.Add(vkRow);
        }

        var keyboard = new
        {
            inline = true,
            buttons = rows,
        };

        return JsonSerializer.Serialize(keyboard);
    }

    private static string MapStyle(ButtonStyle? style) =>
        style switch
        {
            ButtonStyle.Primary => "primary",
            ButtonStyle.Success => "positive",
            ButtonStyle.Danger => "negative",
            _ => "secondary",
        };
}
