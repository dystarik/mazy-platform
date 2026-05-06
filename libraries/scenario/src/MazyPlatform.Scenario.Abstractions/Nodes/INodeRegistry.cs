namespace MazyPlatform.Scenario.Abstractions.Nodes;

using System.Text.Json;

/// <summary>
/// Реестр типов узлов.
/// Связывает строковый тип из JSON с фабрикой создания узла.
/// </summary>
public interface INodeRegistry
{
    /// <summary>
    /// Регистрирует фабрику для типа узла.
    /// </summary>
    /// <param name="nodeType">Строковый тип (например, "send_message").</param>
    /// <param name="factory">
    /// Фабрика: принимает Guid узла и JSON параметров,
    /// возвращает экземпляр узла.
    /// </param>
    void Register(string nodeType, Func<Guid, JsonElement, INode> factory);

    /// <summary>
    /// Создаёт узел по типу и параметрам из JSON.
    /// </summary>
    /// <param name="nodeType">Строковый тип узла.</param>
    /// <param name="nodeId">Идентификатор узла.</param>
    /// <param name="parameters">Параметры узла из JSON сценария.</param>
    /// <returns>Экземпляр узла.</returns>
    INode Resolve(string nodeType, Guid nodeId, JsonElement parameters);

    /// <summary>
    /// Проверяет, зарегистрирован ли тип узла.
    /// </summary>
    /// <param name="nodeType">Строковый тип узла.</param>
    /// <returns>true, если тип зарегистрирован.</returns>
    bool IsRegistered(string nodeType);
}
