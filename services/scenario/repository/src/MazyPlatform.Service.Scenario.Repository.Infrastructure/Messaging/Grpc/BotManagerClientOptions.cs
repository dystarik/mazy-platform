namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Messaging.Grpc;

using System.ComponentModel.DataAnnotations;

internal sealed class BotManagerClientOptions
{
    public const string SectionName = "BotManager";

    [Required]
    public string Address { get; init; } = null!;

    [Required]
    public string AccessToken { get; init; } = null!;
}
