namespace MazyPlatform.Scenario.Telegram.Api;

using System.Net.Http.Json;
using System.Text.Json;

using MazyPlatform.Scenario.Telegram.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
/// Клиент для работы с Telegram Bot API.
/// </summary>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов.</param>
/// <param name="options">Настройки Telegram.</param>
public sealed class TelegramApiClient(IHttpClientFactory httpClientFactory, IOptions<TelegramOptions> options)
{
    private readonly TelegramOptions _options = options.Value;

    /// <summary>
    /// Вызывает метод Telegram Bot API.
    /// </summary>
    /// <param name="method">Имя метода Bot API.</param>
    /// <param name="parameters">Параметры запроса.</param>
    /// <param name="token">Токен Telegram-бота.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>JSON-элемент поля "result" из ответа Telegram.</returns>
    /// <exception cref="TelegramApiException">Если Telegram вернул ошибку.</exception>
    public async Task<JsonElement> CallAsync(string method, object parameters, string token, CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient("TelegramApi");
        using var content = JsonContent.Create(parameters);

        using var response = await client.PostAsync(
            $"{_options.BaseUrl.TrimEnd('/')}/bot{token}/{method}",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;
        var isOk = root.TryGetProperty("ok", out var okElement)
            && okElement.ValueKind == JsonValueKind.True;

        if (!isOk)
        {
            var errorCode = root.TryGetProperty("error_code", out var errorCodeElement)
                ? errorCodeElement.GetInt32()
                : 0;
            var description = root.TryGetProperty("description", out var descriptionElement)
                ? descriptionElement.GetString() ?? "Unknown error"
                : "Unknown error";

            throw new TelegramApiException(errorCode, description);
        }

        return root.TryGetProperty("result", out var result)
            ? result.Clone()
            : root.Clone();
    }

    /// <summary>
    /// Строит URL файла Telegram по пути из getFile.
    /// </summary>
    /// <param name="token">Токен Telegram-бота.</param>
    /// <param name="filePath">Путь файла Telegram.</param>
    /// <returns>URL для скачивания файла.</returns>
    public string GetFileUrl(string token, string filePath) =>
        $"{_options.FileBaseUrl.TrimEnd('/')}/bot{token}/{filePath}";
}
