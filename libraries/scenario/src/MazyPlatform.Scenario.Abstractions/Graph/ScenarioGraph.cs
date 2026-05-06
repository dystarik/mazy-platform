namespace MazyPlatform.Scenario.Abstractions.Graph;

using MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Граф сценария — структура данных, описывающая логику диалога.
/// Не наследуется. Одинаков для всех платформ.
/// </summary>
public sealed class ScenarioGraph
{
    /// <summary>
    /// Идентификатор стартового узла.
    /// </summary>
    public required Guid StartNodeId { get; init; }

    /// <summary>
    /// Все узлы сценария. Ключ — NodeId.
    /// </summary>
    public required IReadOnlyDictionary<Guid, INode> Nodes { get; init; }

    /// <summary>
    /// Все связи сценария. Ключ — FromNodeId.
    /// </summary>
    public required IReadOnlyDictionary<Guid, IReadOnlyList<NodeConnection>> Connections { get; init; }

    /// <summary>
    /// Точки входа по нажатию кнопки.
    /// Ключ — payload кнопки, значение — идентификатор узла receive_button_press.
    /// При навигации по кнопке выполнение начинается с этого узла.
    /// </summary>
    public required IReadOnlyDictionary<string, Guid> EntryPoints { get; init; }

    /// <summary>
    /// Находит следующий узел по текущему узлу и ключу ветки.
    /// </summary>
    /// <param name="fromNodeId">Текущий узел.</param>
    /// <param name="branchKey">Ключ ветки (null → "default").</param>
    /// <returns>Идентификатор следующего узла или null, если связи нет (конец сценария).</returns>
    public Guid? GetNextNodeId(Guid fromNodeId, string? branchKey = null)
    {
        var key = branchKey ?? "default";

        if (!Connections.TryGetValue(fromNodeId, out var connections))
            return null;

        return connections
            .FirstOrDefault(c => string.Equals(c.BranchKey, key, StringComparison.OrdinalIgnoreCase))
            ?.ToNodeId;
    }
}
