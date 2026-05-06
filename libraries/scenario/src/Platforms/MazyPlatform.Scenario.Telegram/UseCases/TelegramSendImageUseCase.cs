namespace MazyPlatform.Scenario.Telegram.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Telegram.Api;

/// <summary>
/// Telegram-реализация отправки изображения.
/// </summary>
/// <param name="apiClient">Клиент Telegram API.</param>
public sealed class TelegramSendImageUseCase(TelegramApiClient apiClient) : ISendImageUseCase
{
    /// <inheritdoc />
    public async Task<SendResult> ExecuteAsync(ExecutionContext context, string imageUrl, string? caption = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["chat_id"] = context.IncomingEvent!.ChatId,
            ["photo"] = imageUrl,
        };

        if (caption is not null)
        {
            parameters["caption"] = caption;
        }

        var response = await apiClient.CallAsync("sendPhoto", parameters, context.BotToken, cancellationToken);
        var messageId = TryGetMessageId(response);

        return new SendResult(new SendImageAction(imageUrl, caption), messageId);
    }

    private static string? TryGetMessageId(JsonElement response) =>
        response.TryGetProperty("message_id", out var messageIdElement)
            ? messageIdElement.GetInt64().ToString(CultureInfo.InvariantCulture)
            : null;
}
