namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация отправки текстового сообщения.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkSendMessageUseCase(VkApiClient apiClient) : ISendMessageUseCase
{
    /// <inheritdoc />
    public async Task<SendResult> ExecuteAsync(ExecutionContext context, string text, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["random_id"] = Random.Shared.Next().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["message"] = text,
        };

        var response = await apiClient.CallAsync("messages.send", parameters, context.BotToken, cancellationToken);
        var messageId = response.ValueKind == System.Text.Json.JsonValueKind.Number ? response.ToString() : null;

        return new SendResult(new SendTextAction(text), messageId);
    }
}
