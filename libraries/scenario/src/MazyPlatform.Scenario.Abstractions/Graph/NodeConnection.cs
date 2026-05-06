namespace MazyPlatform.Scenario.Abstractions.Graph;

/// <summary>
/// Связь между узлами в графе сценария.
/// </summary>
/// <param name="FromNodeId">Идентификатор исходного узла.</param>
/// <param name="ToNodeId">Идентификатор целевого узла.</param>
/// <param name="BranchKey">
/// Ключ ветки ("default", "true", "false").
/// Для обычных узлов — "default".
/// Для условных — "true" или "false".
/// </param>
public sealed record NodeConnection(
    Guid FromNodeId,
    Guid ToNodeId,
    string BranchKey = "default");
