namespace MazyPlatform.Scenario.Telegram.Registration;

using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Telegram.Api;
using MazyPlatform.Scenario.Telegram.Builder;
using MazyPlatform.Scenario.Telegram.Configuration;
using MazyPlatform.Scenario.Telegram.UseCases;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Методы расширения для регистрации Telegram-сервисов сценариев.
/// </summary>
public static class TelegramScenarioServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует API-клиент, юзкейсы и платформенный сборщик Telegram.
    /// Должен вызываться после <c>AddScenario()</c>.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Делегат настройки <see cref="TelegramOptions"/>.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddTelegramScenario(this IServiceCollection services, Action<TelegramOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient("TelegramApi");
        services.AddSingleton<TelegramApiClient>();
        NodeDescriptorScanner.RegisterDescriptorsFromAssembly(services, typeof(TelegramScenarioServiceCollectionExtensions).Assembly);

        RegisterUniversalUseCases(services);
        services.AddSingleton<IScenarioPlatformBuilder, TelegramScenarioPlatformBuilder>();

        return services;
    }

    /// <summary>
    /// Регистрирует Telegram-дескрипторы узлов для каталога.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddTelegramScenarioCatalog(this IServiceCollection services)
    {
        NodeDescriptorScanner.RegisterDescriptorsFromAssembly(services, typeof(TelegramScenarioServiceCollectionExtensions).Assembly);
        return services;
    }

    private static void RegisterUniversalUseCases(IServiceCollection services)
    {
        services.AddSingleton<TelegramSendMessageUseCase>();
        services.AddSingleton<TelegramSendButtonsUseCase>();
        services.AddSingleton<TelegramSendImageUseCase>();
        services.AddSingleton<TelegramReceiveMessageUseCase>();
        services.AddSingleton<TelegramReceiveButtonPressUseCase>();
        services.AddSingleton<TelegramReceiveImageUseCase>();
        services.AddSingleton<TelegramEditMessageUseCase>();
        services.AddSingleton<TelegramTypingIndicatorUseCase>();
        services.AddSingleton<TelegramGetUserInfoUseCase>();
        services.AddSingleton<TelegramDeleteMessageUseCase>();
    }
}
