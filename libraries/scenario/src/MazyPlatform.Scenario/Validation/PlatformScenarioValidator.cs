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
        var limitedProviders = platformProviders.Select(provider => new PlatformLimitedNodeSchemaProvider(provider, platformKey));
        return new ScenarioValidator(limitedProviders).Validate(scenarioJson);
    }

    private sealed class PlatformLimitedNodeSchemaProvider(INodeSchemaProvider inner, string platformKey) : INodeSchemaProvider
    {
        public string Type => inner.Type;

        public IReadOnlyList<NodeParamSchema> Schema { get; } =
            PlatformButtonLimitResolver.Apply(inner.Type, inner.Schema, platformKey);

        public IReadOnlyList<string> Validate(Guid nodeId, System.Text.Json.JsonElement parameters)
        {
            var errors = inner.Validate(nodeId, parameters).ToList();
            NodeParamLimitValidator.ValidateLimits(nodeId, parameters, Schema, errors);
            return errors;
        }
    }
}
