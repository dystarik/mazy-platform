namespace MazyPlatform.Scenario.Registration;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.Scenario.Abstractions.Validation;
using MazyPlatform.Scenario.Builder;
using MazyPlatform.Scenario.Executor;
using MazyPlatform.Scenario.Registry;
using MazyPlatform.Scenario.Validation;
using MazyPlatform.Scenario.Validators.Input;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Методы расширения для регистрации сервисов библиотеки Scenario.
/// </summary>
public static class ScenarioServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы библиотеки Scenario: реестр узлов,
    /// каталог, сборщик, исполнитель и валидаторы ввода.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddScenario(this IServiceCollection services)
    {
        services.AddHttpClient("ScenarioHttp");
        NodeDescriptorScanner.RegisterDescriptorsFromAssembly(services, typeof(ScenarioServiceCollectionExtensions).Assembly);

        if (services.All(s => s.ServiceType != typeof(INodeRegistry)))
        {
            services.AddSingleton<INodeRegistry>(sp =>
            {
                var registry = new NodeRegistry();
                RegisterDescriptorNodes(registry, sp.GetServices<INodeDescriptor>(), sp);
                return registry;
            });
        }

        if (services.All(s => s.ServiceType != typeof(INodeCatalog)))
        {
            services.AddSingleton<INodeCatalog>(sp =>
            {
                var catalog = new NodeCatalog();
                RegisterDescriptorSchemas(catalog, sp.GetServices<INodeSchemaProvider>());
                return catalog;
            });
        }

        services.AddScoped<IScenarioBuilder, ScenarioBuilder>();
        services.AddScoped<IScenarioExecutor, ScenarioExecutor>();
        services.AddSingleton<IScenarioPlatformBuilderResolver, ScenarioPlatformBuilderResolver>();

        if (services.All(s => s.ServiceType != typeof(IPlatformNodeCatalog)))
        {
            services.AddSingleton<IPlatformNodeCatalog, PlatformNodeCatalog>();
        }

        if (services.All(s => s.ServiceType != typeof(IScenarioValidator)))
        {
            services.AddSingleton<IScenarioValidator, ScenarioValidator>();
        }

        if (services.All(s => s.ServiceType != typeof(IPlatformScenarioValidator)))
        {
            services.AddSingleton<IPlatformScenarioValidator, PlatformScenarioValidator>();
        }

        RegisterInputValidators(services);

        return services;
    }

    /// <summary>
    /// Регистрирует только каталог узлов сценария (<see cref="INodeCatalog"/>)
    /// без исполнителя, сборщика и юзкейсов.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddScenarioCatalog(this IServiceCollection services)
    {
        NodeDescriptorScanner.RegisterDescriptorsFromAssembly(services, typeof(ScenarioServiceCollectionExtensions).Assembly);

        if (services.All(s => s.ServiceType != typeof(INodeCatalog)))
        {
            services.AddSingleton<INodeCatalog>(sp =>
            {
                var catalog = new NodeCatalog();
                RegisterDescriptorSchemas(catalog, sp.GetServices<INodeSchemaProvider>());
                return catalog;
            });
        }

        if (services.All(s => s.ServiceType != typeof(IScenarioValidator)))
        {
            services.AddSingleton<IScenarioValidator, ScenarioValidator>();
        }

        if (services.All(s => s.ServiceType != typeof(IPlatformNodeCatalog)))
        {
            services.AddSingleton<IPlatformNodeCatalog, PlatformNodeCatalog>();
        }

        if (services.All(s => s.ServiceType != typeof(IPlatformScenarioValidator)))
        {
            services.AddSingleton<IPlatformScenarioValidator, PlatformScenarioValidator>();
        }

        return services;
    }

    private static void RegisterDescriptorNodes(NodeRegistry registry, IEnumerable<INodeDescriptor> descriptors, IServiceProvider sp)
    {
        foreach (var descriptor in descriptors)
        {
            registry.Register(descriptor.Type, (id, parameters) => descriptor.Create(id, parameters, sp));
        }
    }

    private static void RegisterDescriptorSchemas(NodeCatalog catalog, IEnumerable<INodeSchemaProvider> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            catalog.Register(descriptor.Type, descriptor.Schema);
        }
    }

    private static void RegisterInputValidators(IServiceCollection services)
    {
        services.AddSingleton<IInputValidator, PhoneInputValidator>();
        services.AddSingleton<IInputValidator, EmailInputValidator>();
        services.AddSingleton<IInputValidator, NumberInputValidator>();
        services.AddSingleton<IInputValidator, RegexInputValidator>();
    }
}
