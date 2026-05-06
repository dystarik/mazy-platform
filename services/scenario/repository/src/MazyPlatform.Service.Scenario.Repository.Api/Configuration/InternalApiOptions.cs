namespace MazyPlatform.Service.Scenario.Repository.Api.Configuration;

using System.ComponentModel.DataAnnotations;

internal sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    [Required]
    public string AccessToken { get; init; } = null!;
}
