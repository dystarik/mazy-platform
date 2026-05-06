namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Каталог метаданных узлов с учётом платформы.
/// </summary>
public interface IPlatformNodeCatalog
{
    /// <summary>
    /// Возвращает метаданные узлов, доступных для указанной платформы.
    /// </summary>
    /// <param name="platformKey">Ключ платформы: "universal", "vk", "telegram".</param>
    /// <returns>Метаданные доступных узлов.</returns>
    IReadOnlyList<NodeMeta> GetAllMeta(string platformKey);
}
