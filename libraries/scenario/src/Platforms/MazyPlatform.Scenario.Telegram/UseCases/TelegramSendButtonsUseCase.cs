namespace MazyPlatform.Scenario.Telegram.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация отправки сообщения с inline-кнопками.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramSendButtonsUseCase(TelegramApiClient apiClient) : ISendButtonsUseCase
{
    /// <inheritdoc />
    public async Task<SendResult> ExecuteAsync(
        ExecutionContext context,
        string text,
        ButtonLayout buttons,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            chat_id = context.IncomingEvent!.ChatId,
            text,
            reply_markup = TelegramInlineKeyboardBuilder.Build(buttons),
        };

        var response = await apiClient.CallAsync("sendMessage", parameters, context.BotToken, cancellationToken);
        var messageId = TryGetMessageId(response);

        return new SendResult(new SendButtonsAction(text, buttons), messageId);
    }

    private static string? TryGetMessageId(JsonElement response) =>
        response.TryGetProperty("message_id", out var messageIdElement)
            ? messageIdElement.GetInt64().ToString(CultureInfo.InvariantCulture)
            : null;
}
