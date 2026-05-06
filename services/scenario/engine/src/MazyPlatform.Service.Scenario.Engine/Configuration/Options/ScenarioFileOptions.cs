namespace MazyPlatform.Service.Scenario.Engine.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class ScenarioFileOptions
{
    public const string SectionName = "ScenarioFile";

    /// <summary>
    /// Путь к JSON-файлу сценария.
    /// TODO: MVP — заменить на gRPC-запрос к service-scenario-repository.
    /// </summary>
    [Required]
    public required string Path { get; set; }
}
