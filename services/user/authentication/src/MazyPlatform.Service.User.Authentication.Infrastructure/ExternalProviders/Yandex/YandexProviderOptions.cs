namespace MazyPlatform.Service.User.Authentication.Infrastructure.ExternalProviders.Yandex;

using System.ComponentModel.DataAnnotations;

internal sealed class YandexProviderOptions
{
    public const string SectionName = "ExternalProviders:Yandex";

    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string ClientId { get; init; }

    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string ClientSecret { get; init; }

    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string RedirectUri { get; init; }
}
