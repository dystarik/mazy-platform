namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Метаданные типа узла для каталога.
/// </summary>
/// <param name="Type">Строковый тип узла (например "send_message"). Используется фронтом как ключ локализации nodes.{Type}.display_name.</param>
/// <param name="Schema">Схема параметров узла. Каждый параметр описывает ключ, тип и обязательность для построения формы в редакторе.</param>
public sealed record NodeMeta(
    string Type,
    IReadOnlyList<NodeParamSchema> Schema);
