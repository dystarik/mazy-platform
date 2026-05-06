namespace MazyPlatform.Scenario.Vk.Registration;

using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Vk.Api;
using MazyPlatform.Scenario.Vk.Builder;
using MazyPlatform.Scenario.Vk.Configuration;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Методы расширения для регистрации VK-сервисов сценариев.
/// </summary>
public static class VkScenarioServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы VK-интеграции: API-клиент,
    /// юзкейсы и платформенные узлы.
    /// Должен вызываться после <c>AddScenario()</c>.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Делегат настройки <see cref="VkOptions"/>.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddVkScenario(this IServiceCollection services, Action<VkOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient("VkApi");
        services.AddHttpClient("VkUpload");
        services.AddSingleton<VkApiClient>();
        NodeDescriptorScanner.RegisterDescriptorsFromAssembly(services, typeof(VkScenarioServiceCollectionExtensions).Assembly);

        RegisterUniversalUseCases(services);
        RegisterPlatformUseCases(services);
        services.AddSingleton<IScenarioPlatformBuilder, VkScenarioPlatformBuilder>();

        return services;
    }

    /// <summary>
    /// Регистрирует VK-дескрипторы узлов для каталога.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddVkScenarioCatalog(this IServiceCollection services)
    {
        NodeDescriptorScanner.RegisterDescriptorsFromAssembly(services, typeof(VkScenarioServiceCollectionExtensions).Assembly);
        return services;
    }

    private static void RegisterUniversalUseCases(IServiceCollection services)
    {
        services.AddSingleton<VkSendMessageUseCase>();
        services.AddSingleton<VkSendButtonsUseCase>();
        services.AddSingleton<VkSendImageUseCase>();
        services.AddSingleton<VkReceiveMessageUseCase>();
        services.AddSingleton<VkReceiveButtonPressUseCase>();
        services.AddSingleton<VkReceiveImageUseCase>();
        services.AddSingleton<VkEditMessageUseCase>();
        services.AddSingleton<VkTypingIndicatorUseCase>();
        services.AddSingleton<VkGetUserInfoUseCase>();
        services.AddSingleton<VkDeleteMessageUseCase>();
    }

    private static void RegisterPlatformUseCases(IServiceCollection services)
    {
        services.AddSingleton<VkSendKeyboardUseCase>();
        services.AddSingleton<VkSendCarouselUseCase>();
        services.AddSingleton<VkRemoveKeyboardUseCase>();
    }
}
