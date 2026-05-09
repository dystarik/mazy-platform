namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация редактирования сообщения.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkEditMessageUseCase(VkApiClient apiClient) : IEditMessageUseCase
{
    /// <inheritdoc />
    public async Task<IOutgoingAction> ExecuteAsync(
        ExecutionContext context,
        string messageId,
        string newText,
        ButtonLayout? buttons,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["message_id"] = messageId,
            ["message"] = newText,
        };

        if (buttons is not null)
        {
            parameters["keyboard"] = BuildKeyboardJson(buttons);
        }

        await apiClient.CallAsync("messages.edit", parameters, context.BotToken, cancellationToken);

        return new EditMessageAction(messageId, newText, buttons);
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
