namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Каталог типов узлов сценария.
/// Хранит схемы параметров для построения редактора и документации.
/// Не используется при исполнении сценария.
/// </summary>
public interface INodeCatalog
{
    /// <summary>
    /// Регистрирует схему параметров для типа узла.
    /// </summary>
    /// <param name="nodeType">Строковый тип узла (например "send_message").</param>
    /// <param name="schema">Схема параметров узла.</param>
    void Register(string nodeType, IReadOnlyList<NodeParamSchema> schema);

    /// <summary>
    /// Возвращает метаданные всех зарегистрированных типов узлов,
    /// отсортированные по типу.
    /// </summary>
    /// <returns>Список метаданных узлов.</returns>
    IReadOnlyList<NodeMeta> GetAllMeta();
}
