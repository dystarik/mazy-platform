namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Описание параметра узла сценария.
/// </summary>
/// <param name="Key">Ключ параметра в JSON (например "text", "messageIdVariable").</param>
/// <param name="Type">Тип значения параметра.</param>
/// <param name="IsRequired">Является ли параметр обязательным.</param>
/// <param name="Description">
/// Человекочитаемое описание параметра для UI редактора (тултип на форме).
/// Опционально, но рекомендуется заполнять для всех публичных параметров.
/// </param>
/// <param name="Fields">Схема вложенных полей для Object/ObjectList/ObjectMatrix.</param>
/// <param name="EnumValues">Допустимые значения для Enum.</param>
/// <param name="AllowEmptyCollection">Разрешать пустой ObjectList/ObjectMatrix.</param>
public sealed record NodeParamSchema(
    string Key,
    NodeParamType Type,
    bool IsRequired,
    string? Description = null,
    IReadOnlyList<NodeParamSchema>? Fields = null,
    IReadOnlyList<string>? EnumValues = null,
    bool AllowEmptyCollection = false);
