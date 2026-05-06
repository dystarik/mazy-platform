namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Помечает дескриптор узла как доступный только для конкретных платформ.
/// Дескрипторы без этого интерфейса считаются универсальными.
/// </summary>
public interface IPlatformScopedNodeDescriptor
{
    /// <summary>
    /// Ключи платформ, для которых доступен дескриптор.
    /// </summary>
    IReadOnlySet<string> PlatformKeys { get; }
}
