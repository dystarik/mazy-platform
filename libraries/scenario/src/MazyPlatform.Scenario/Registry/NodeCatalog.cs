namespace MazyPlatform.Scenario.Registry;

using MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Реализация <see cref="INodeCatalog"/>.
/// Хранит схемы параметров узлов для построения каталога редактора.
/// </summary>
public sealed class NodeCatalog : INodeCatalog
{
    private readonly Dictionary<string, IReadOnlyList<NodeParamSchema>> _schemas = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Register(string nodeType, IReadOnlyList<NodeParamSchema> schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeType);
        ArgumentNullException.ThrowIfNull(schema);

        if (_schemas.ContainsKey(nodeType))
        {
            throw new InvalidOperationException(
                $"Тип узла \"{nodeType}\" уже зарегистрирован в NodeCatalog. " +
                "Возможно, узел зарегистрирован одновременно через дескриптор и через ручную регистрацию. " +
                "Удалите ручную регистрацию.");
        }

        _schemas[nodeType] = schema;
    }

    /// <inheritdoc />
    public IReadOnlyList<NodeMeta> GetAllMeta() =>
        [.. _schemas.Keys.Order(StringComparer.Ordinal).Select(type => new NodeMeta(type, _schemas[type]))];
}
