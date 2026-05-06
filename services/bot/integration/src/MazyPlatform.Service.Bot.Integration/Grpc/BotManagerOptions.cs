namespace MazyPlatform.Service.Bot.Integration.Grpc;

using System.ComponentModel.DataAnnotations;

internal sealed class BotManagerOptions
{
    public const string SectionName = "BotManager";

    [Required]
    public required string Address { get; init; }

    [Required]
    public required string AccessToken { get; init; }
}
