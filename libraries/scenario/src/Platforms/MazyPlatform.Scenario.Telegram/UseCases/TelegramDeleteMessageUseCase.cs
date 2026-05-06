namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация удаления сообщения.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramDeleteMessageUseCase(TelegramApiClient apiClient) : IDeleteMessageUseCase
{
    /// <inheritdoc />
    public async Task<IOutgoingAction> ExecuteAsync(ExecutionContext context, string messageId, CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            chat_id = context.IncomingEvent!.ChatId,
            message_id = ParseMessageId(messageId),
        };

        await apiClient.CallAsync("deleteMessage", parameters, context.BotToken, cancellationToken);

        return new DeleteMessageAction(messageId);
    }

    private static object ParseMessageId(string messageId) =>
        long.TryParse(messageId, System.Globalization.CultureInfo.InvariantCulture, out var parsedMessageId)
            ? parsedMessageId
            : messageId;
}
