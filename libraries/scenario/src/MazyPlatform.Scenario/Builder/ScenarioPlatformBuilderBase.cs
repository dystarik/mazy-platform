namespace MazyPlatform.Scenario.Builder;

using MazyPlatform.Scenario.Abstractions.Graph;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Platforms;
using MazyPlatform.Scenario.Registry;

/// <summary>
/// Базовый сборщик графа сценария для платформенных интеграций.
/// </summary>
public abstract class ScenarioPlatformBuilderBase(
    IServiceProvider serviceProvider,
    IEnumerable<INodeDescriptor> descriptors) : IScenarioPlatformBuilder
{
    private readonly IReadOnlyList<INodeDescriptor> _descriptors = [.. descriptors];

    /// <inheritdoc />
    public abstract string PlatformKey { get; }

    /// <summary>
    /// Маппинг универсальных интерфейсов юзкейсов на платформенные реализации.
    /// </summary>
    protected abstract IReadOnlyDictionary<Type, Type> UseCaseTypeMap { get; }

    /// <inheritdoc />
    public ScenarioGraph Build(string scenarioJson)
    {
        var registry = new NodeRegistry();
        var platformProvider = new PlatformServiceProvider(serviceProvider, UseCaseTypeMap);
        var platformDescriptors = PlatformDescriptorFilter.Filter(_descriptors, PlatformKey);

        foreach (var descriptor in platformDescriptors)
        {
            registry.Register(descriptor.Type, (id, parameters) => descriptor.Create(id, parameters, platformProvider));
        }

        return new ScenarioBuilder(registry).Build(scenarioJson);
    }
}
