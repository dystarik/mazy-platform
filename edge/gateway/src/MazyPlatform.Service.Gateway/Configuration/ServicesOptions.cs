namespace MazyPlatform.Service.Gateway.Configuration;

internal sealed class ServicesOptions
{
    public const string SectionName = "Services";

    public string Authentication { get; init; } = string.Empty;

    public string ScenarioRepository { get; init; } = string.Empty;

    public string BotManager { get; init; } = string.Empty;
}
