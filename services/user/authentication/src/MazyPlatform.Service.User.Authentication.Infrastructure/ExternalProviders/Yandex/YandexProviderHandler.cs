namespace MazyPlatform.Service.User.Authentication.Infrastructure.ExternalProviders.Yandex;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using Microsoft.Extensions.Options;

internal sealed class YandexProviderHandler(
    IOptions<YandexProviderOptions> options,
    IHttpClientFactory httpClientFactory) : IProviderHandler
{
    private readonly YandexProviderOptions _options = options.Value;

    public ExternalProviderType ProviderType => ExternalProviderType.Yandex;

    public async Task<Email?> GetEmailAsync(string code, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(code, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        return await GetEmailByTokenAsync(accessToken, cancellationToken);
    }

    private async Task<string?> GetAccessTokenAsync(string code, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = _options.RedirectUri,
        };

        var httpClient = httpClientFactory.CreateClient("Yandex");
        var response = await httpClient.PostAsync(
            "https://oauth.yandex.com/token",
            new FormUrlEncodedContent(parameters),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<YandexTokenResponse>(cancellationToken: cancellationToken);
        return result?.AccessToken;
    }

    private async Task<Email?> GetEmailByTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://login.yandex.ru/info?format=json");

        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

        var httpClient = httpClientFactory.CreateClient("Yandex");
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<YandexUserInfoResponse>(cancellationToken: cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Email))
            return null;

        var emailResult = Email.Create(result.Email);
        if (emailResult.IsFailure)
            return null;

        return emailResult.Value;
    }

    private sealed record YandexUserInfoResponse(
        [property: JsonPropertyName("default_email")] string? Email);

    private sealed record YandexTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken);
}
