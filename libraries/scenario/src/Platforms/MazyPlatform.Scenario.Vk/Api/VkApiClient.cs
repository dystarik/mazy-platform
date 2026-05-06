namespace MazyPlatform.Scenario.Vk.Api;

using System.Text.Json;

using MazyPlatform.Scenario.Vk.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
/// Клиент для работы с VK Community API.
/// </summary>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов.</param>
/// <param name="options">Настройки VK.</param>
public sealed class VkApiClient(IHttpClientFactory httpClientFactory, IOptions<VkOptions> options)
{
    private const string _baseUrl = "https://api.vk.com/method/";

    private readonly string _apiVersion = options.Value.ApiVersion;

    /// <summary>
    /// Вызывает метод VK API.
    /// </summary>
    /// <param name="method">Имя метода (например, "messages.send").</param>
    /// <param name="parameters">Параметры запроса.</param>
    /// <param name="token">Токен сообщества VK.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>JSON-элемент поля "response" из ответа VK.</returns>
    /// <exception cref="VkApiException">Если VK API вернул ошибку.</exception>
    public async Task<JsonElement> CallAsync(string method, IDictionary<string, string> parameters, string token, CancellationToken cancellationToken = default)
    {
        var requestParams = new Dictionary<string, string>(parameters, StringComparer.Ordinal)
        {
            ["access_token"] = token,
            ["v"] = _apiVersion,
        };

        using var client = httpClientFactory.CreateClient("VkApi");
        using var content = new FormUrlEncodedContent(requestParams);

        using var response = await client.PostAsync(
            $"{_baseUrl}{method}",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error))
        {
            var errorCode = error.GetProperty("error_code").GetInt32();
            var errorMsg = error.GetProperty("error_msg").GetString() ?? "Unknown error";
            throw new VkApiException(errorCode, errorMsg);
        }

        return root.GetProperty("response").Clone();
    }
}
