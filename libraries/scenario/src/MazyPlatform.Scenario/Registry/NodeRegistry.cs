namespace MazyPlatform.Scenario.Registry;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Реестр типов узлов.
/// Хранит маппинг "строковый тип из JSON → фабрика создания узла".
/// </summary>
public sealed class NodeRegistry : INodeRegistry
{
    private readonly Dictionary<string, Func<Guid, JsonElement, INode>> _factories = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Register(string nodeType, Func<Guid, JsonElement, INode> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeType);
        ArgumentNullException.ThrowIfNull(factory);

        if (_factories.ContainsKey(nodeType))
        {
            throw new InvalidOperationException(
                $"Тип узла \"{nodeType}\" уже зарегистрирован в NodeRegistry. " +
                "Возможно, узел зарегистрирован одновременно через дескриптор и через ручную регистрацию. " +
                "Удалите ручную регистрацию.");
        }

        _factories[nodeType] = factory;
    }

    /// <inheritdoc />
    public INode Resolve(string nodeType, Guid nodeId, JsonElement parameters)
    {
        if (!_factories.TryGetValue(nodeType, out var factory))
        {
            throw new InvalidOperationException(
                $"Тип узла \"{nodeType}\" не зарегистрирован в реестре. " +
                "Убедитесь, что платформа зарегистрирована через DI.");
        }

        return factory(nodeId, parameters);
    }

    /// <inheritdoc />
    public bool IsRegistered(string nodeType) => _factories.ContainsKey(nodeType);
}
