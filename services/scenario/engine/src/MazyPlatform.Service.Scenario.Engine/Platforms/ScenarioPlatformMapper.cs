namespace MazyPlatform.Service.Scenario.Engine.Platforms;

using MazyPlatform.Contracts.Bot;
using MazyPlatform.Contracts.Bot.Grpc.Manager;
using MazyPlatform.Scenario.Abstractions.Platforms;

internal static class ScenarioPlatformMapper
{
    public static IReadOnlyList<BotPlatformType> SupportedGrpcPlatforms { get; } =
    [
        BotPlatformType.Vk,
        BotPlatformType.Telegram,
    ];

    public static bool IsSupported(PlatformType platformType) =>
        platformType is PlatformType.Vk or PlatformType.Telegram;

    public static bool IsTelegram(PlatformType platformType) => platformType == PlatformType.Telegram;

    public static string ToScenarioPlatformKey(PlatformType platformType) => platformType switch
    {
        PlatformType.Vk => ScenarioPlatformKeys.Vk,
        PlatformType.Telegram => ScenarioPlatformKeys.Telegram,
        _ => throw new ArgumentException($"Неподдерживаемая платформа: {platformType}", nameof(platformType)),
    };

    public static PlatformType ToBotContractPlatform(BotPlatformType platformType) => platformType switch
    {
        BotPlatformType.Vk => PlatformType.Vk,
        BotPlatformType.Telegram => PlatformType.Telegram,
        _ => throw new ArgumentException($"Неподдерживаемая платформа: {platformType}", nameof(platformType)),
    };

    public static BotPlatformType ToGrpcPlatform(PlatformType platformType) => platformType switch
    {
        PlatformType.Vk => BotPlatformType.Vk,
        PlatformType.Telegram => BotPlatformType.Telegram,
        _ => throw new ArgumentException($"Неподдерживаемая платформа: {platformType}", nameof(platformType)),
    };
}
