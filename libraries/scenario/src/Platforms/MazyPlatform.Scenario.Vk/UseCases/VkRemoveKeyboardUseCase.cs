namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk;
using MazyPlatform.Scenario.Vk.Actions;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// Юзкейс удаления reply-клавиатуры VK.
/// Отправляет сообщение с пустым массивом кнопок — VK скрывает текущую клавиатуру.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
public sealed class VkRemoveKeyboardUseCase(VkApiClient apiClient)
{
    private const string _emptyKeyboardJson = "{\"buttons\":[]}";

    /// <summary>
    /// Отправляет сообщение с пустой клавиатурой, скрывая ранее показанную клавиатуру.
    /// </summary>
    /// <param name="context">Контекст выполнения.</param>
    /// <param name="text">Текст сообщения (обязателен — VK API не принимает messages.send без message и без attachments).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат отправки с действием и опциональным идентификатором сообщения.</returns>
    public async Task<SendResult> ExecuteAsync(
        ExecutionContext context,
        string text,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = context.IncomingEvent!.ChatId,
            ["random_id"] = Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["message"] = text,
            ["keyboard"] = _emptyKeyboardJson,
        };

        var response = await apiClient.CallAsync("messages.send", parameters, context.BotToken, cancellationToken);
        var messageId = VkMessageId.FromSendResponse(response);

        return new SendResult(new VkRemoveKeyboardAction(text), messageId);
    }
}
