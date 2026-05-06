namespace MazyPlatform.Scenario.Telegram.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация показа индикатора набора текста.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramTypingIndicatorUseCase(TelegramApiClient apiClient) : ITypingIndicatorUseCase
{
    /// <inheritdoc />
    public async Task<IOutgoingAction> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            chat_id = context.IncomingEvent!.ChatId,
            action = "typing",
        };

        await apiClient.CallAsync("sendChatAction", parameters, context.BotToken, cancellationToken);

        return new TypingIndicatorAction();
    }
}
