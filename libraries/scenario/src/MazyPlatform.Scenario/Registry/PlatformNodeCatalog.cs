namespace MazyPlatform.Scenario.Registry;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Platforms;

/// <summary>
/// Каталог узлов с фильтрацией по платформе.
/// </summary>
public sealed class PlatformNodeCatalog(IEnumerable<INodeSchemaProvider> schemaProviders) : IPlatformNodeCatalog
{
    private readonly IReadOnlyList<INodeSchemaProvider> _schemaProviders = [.. schemaProviders];

    /// <inheritdoc />
    public IReadOnlyList<NodeMeta> GetAllMeta(string platformKey) =>
        [.. PlatformDescriptorFilter
            .Filter(_schemaProviders, platformKey)
            .OrderBy(provider => provider.Type, StringComparer.Ordinal)
            .Select(provider => new NodeMeta(provider.Type, provider.Schema))];
}
