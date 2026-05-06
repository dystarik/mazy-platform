namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Messaging.Grpc;

using System.ComponentModel.DataAnnotations;

internal sealed class ScenarioRepositoryClientOptions
{
    public const string SectionName = "ScenarioRepository";

    [Required]
    public string Address { get; init; } = null!;

    [Required]
    public string AccessToken { get; init; } = null!;
}
