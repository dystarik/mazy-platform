namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Extensions;

using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects.ValueObjects;

internal static class PlatformTypeExtensions
{
    public static string ToScenarioPlatformKey(this PlatformType platformType) => platformType switch
    {
        PlatformType.Universal => ScenarioPlatformKeys.Universal,
        PlatformType.Vk => ScenarioPlatformKeys.Vk,
        PlatformType.Telegram => ScenarioPlatformKeys.Telegram,
        _ => throw new ArgumentException($"Неподдерживаемый тип платформы: {platformType}", nameof(platformType)),
    };
}
