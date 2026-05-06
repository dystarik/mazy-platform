namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация редактирования сообщения.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramEditMessageUseCase(TelegramApiClient apiClient) : IEditMessageUseCase
{
    /// <inheritdoc />
    public async Task<IOutgoingAction> ExecuteAsync(
        ExecutionContext context,
        string messageId,
        string newText,
        ButtonLayout? buttons,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["chat_id"] = context.IncomingEvent!.ChatId,
            ["message_id"] = ParseMessageId(messageId),
            ["text"] = newText,
        };

        if (buttons is not null)
        {
            parameters["reply_markup"] = TelegramInlineKeyboardBuilder.Build(buttons);
        }

        await apiClient.CallAsync("editMessageText", parameters, context.BotToken, cancellationToken);

        return new EditMessageAction(messageId, newText, buttons);
    }

    private static object ParseMessageId(string messageId) =>
        long.TryParse(messageId, System.Globalization.CultureInfo.InvariantCulture, out var parsedMessageId)
            ? parsedMessageId
            : messageId;
}
