namespace MazyPlatform.Scenario.Validation;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.Scenario.Platforms;

/// <summary>
/// Валидатор сценария с фильтрацией узлов по платформе.
/// </summary>
public sealed class PlatformScenarioValidator(IEnumerable<INodeSchemaProvider> schemaProviders) : IPlatformScenarioValidator
{
    private readonly IReadOnlyList<INodeSchemaProvider> _schemaProviders = [.. schemaProviders];

    /// <inheritdoc />
    public ScenarioValidationResult Validate(string scenarioJson, string platformKey)
    {
        var platformProviders = PlatformDescriptorFilter.Filter(_schemaProviders, platformKey);
        return new ScenarioValidator(platformProviders).Validate(scenarioJson);
    }
}
