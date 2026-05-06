namespace MazyPlatform.Scenario.Telegram.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация отправки текстового сообщения.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramSendMessageUseCase(TelegramApiClient apiClient) : ISendMessageUseCase
{
    /// <inheritdoc />
    public async Task<SendResult> ExecuteAsync(ExecutionContext context, string text, CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            chat_id = context.IncomingEvent!.ChatId,
            text,
        };

        var response = await apiClient.CallAsync("sendMessage", parameters, context.BotToken, cancellationToken);
        var messageId = TryGetMessageId(response);

        return new SendResult(new SendTextAction(text), messageId);
    }

    private static string? TryGetMessageId(JsonElement response) =>
        response.TryGetProperty("message_id", out var messageIdElement)
            ? messageIdElement.GetInt64().ToString(CultureInfo.InvariantCulture)
            : null;
}
