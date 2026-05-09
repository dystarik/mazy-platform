namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk;
using MazyPlatform.Scenario.Vk.Actions;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// Юзкейс отправки VK reply-клавиатуры под полем ввода.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkSendKeyboardUseCase(VkApiClient apiClient)
{
    /// <summary>
    /// Отправляет сообщение с VK reply-клавиатурой.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="buttonRows">Строки кнопок (массив массивов).</param>
    /// <param name="oneTime">Скрывать клавиатуру после нажатия.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отправки клавиатуры с действием и опциональным идентификатором сообщения.</returns>
    public async Task<SendResult> ExecuteAsync(
        ExecutionContext context,
        string text,
        IReadOnlyList<IReadOnlyList<VkButtonInfo>> buttonRows,
        bool oneTime,
        CancellationToken cancellationToken = default)
    {
        var keyboardJson = BuildKeyboardJson(buttonRows, oneTime);

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["random_id"] = Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["message"] = text,
            ["keyboard"] = keyboardJson,
        };

        var response = await apiClient.CallAsync("messages.send", parameters, context.BotToken, cancellationToken);
        var messageId = VkMessageId.FromSendResponse(response);

        return new SendResult(new VkSendKeyboardAction(text, keyboardJson), messageId);
    }

    private static string BuildKeyboardJson(IReadOnlyList<IReadOnlyList<VkButtonInfo>> buttonRows, bool oneTime)
    {
        var rows = new List<List<object>>(buttonRows.Count);

        foreach (var row in buttonRows)
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
                        payload = VkButtonPayloadFormatter.Format(button.Payload),
                    },
                    color = button.Color ?? "primary",
                });
            }

            rows.Add(vkRow);
        }

        object keyboard = new
        {
            one_time = oneTime,
            inline = false,
            buttons = rows,
        };

        return JsonSerializer.Serialize(keyboard);
    }
}
