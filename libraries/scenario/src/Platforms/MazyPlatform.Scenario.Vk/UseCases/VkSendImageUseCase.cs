namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Vk;
using MazyPlatform.Scenario.Vk.Api;

/// <summary>
/// VK-реализация отправки изображения.
/// </summary>
/// <param name="apiClient">Клиент VK API.</param>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов для загрузки изображений.</param>
public sealed class VkSendImageUseCase(VkApiClient apiClient, IHttpClientFactory httpClientFactory) : ISendImageUseCase
{
    /// <inheritdoc />
    public async Task<SendResult> ExecuteAsync(ExecutionContext context, string imageUrl, string? caption = null, CancellationToken cancellationToken = default)
    {
        var peerId = context.IncomingEvent!.ChatId;
        var botToken = context.BotToken;

        var uploadUrl = await GetUploadUrlAsync(peerId, botToken, cancellationToken);
        var uploadResult = await UploadPhotoAsync(uploadUrl, imageUrl, cancellationToken);
        var attachment = await SavePhotoAsync(uploadResult, botToken, cancellationToken);

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = peerId,
            ["random_id"] = Random.Shared.Next().ToString(CultureInfo.InvariantCulture),
            ["attachment"] = attachment,
        };

        if (caption is not null)
        {
            parameters["message"] = caption;
        }

        var response = await apiClient.CallAsync("messages.send", parameters, botToken, cancellationToken);
        var messageId = VkMessageId.FromSendResponse(response);

        return new SendResult(new SendImageAction(imageUrl, caption), messageId);
    }

    private async Task<string> GetUploadUrlAsync(string peerId, string botToken, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peer_id"] = peerId,
        };

        var response = await apiClient.CallAsync("photos.getMessagesUploadServer", parameters, botToken, cancellationToken);

        return response.GetProperty("upload_url").GetString()!;
    }

    private async Task<JsonElement> UploadPhotoAsync(string uploadUrl, string imageUrl, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("VkUpload");
        await using var imageStream = await client.GetStreamAsync(imageUrl, cancellationToken);
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        using var formData = new MultipartFormDataContent();
        formData.Add(streamContent, "photo", "photo.jpg");

        using var uploadResponse = await client.PostAsync(uploadUrl, formData, cancellationToken);
        uploadResponse.EnsureSuccessStatusCode();

        var json = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private async Task<string> SavePhotoAsync(JsonElement uploadResult, string botToken, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["photo"] = uploadResult.GetProperty("photo").GetString()!,
            ["server"] = uploadResult.GetProperty("server").GetRawText(),
            ["hash"] = uploadResult.GetProperty("hash").GetString()!,
        };

        var response = await apiClient.CallAsync("photos.saveMessagesPhoto", parameters, botToken, cancellationToken);

        var photo = response[0];
        var ownerId = photo.GetProperty("owner_id").GetInt64();
        var photoId = photo.GetProperty("id").GetInt64();

        return $"photo{ownerId.ToString(CultureInfo.InvariantCulture)}_{photoId.ToString(CultureInfo.InvariantCulture)}";
    }
}
