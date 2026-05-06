namespace MazyPlatform.Service.Scenario.Engine.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class ScenarioRepositoryOptions
{
    public const string SectionName = "ScenarioRepository";

    [Required]
    public string Address { get; init; } = null!;
}
