namespace MazyPlatform.Scenario.Vk.UseCases;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация показа индикатора набора текста.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkTypingIndicatorUseCase(VkApiClient apiClient) : ITypingIndicatorUseCase
{
    /// <inheritdoc />
    public async Task<IOutgoingAction> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["type"] = "typing",
        };

        await apiClient.CallAsync("messages.setActivity", parameters, context.BotToken, cancellationToken);

        return new TypingIndicatorAction();
    }
}
