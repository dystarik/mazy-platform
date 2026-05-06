namespace MazyPlatform.Service.Scenario.Engine.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class BotManagerOptions
{
    public const string SectionName = "BotManager";

    [Required]
    public string Address { get; init; } = null!;

    [Required]
    public string AccessToken { get; init; } = null!;
}
