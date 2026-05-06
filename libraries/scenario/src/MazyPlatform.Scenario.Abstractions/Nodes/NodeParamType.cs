namespace MazyPlatform.Scenario.Abstractions.Nodes;

/// <summary>
/// Тип параметра узла сценария.
/// </summary>
public enum NodeParamType
{
    /// <summary>
    /// Строковое значение.
    /// </summary>
    String,

    /// <summary>
    /// Целочисленное значение.
    /// </summary>
    Int,

    /// <summary>
    /// Логическое значение.
    /// </summary>
    Bool,

    /// <summary>
    /// Словарь строковых значений (ключ-значение).
    /// </summary>
    StringDictionary,

    /// <summary>
    /// Список строковых значений.
    /// </summary>
    StringList,

    /// <summary>
    /// Структурный объект с фиксированным набором полей.
    /// Поля описываются через NodeParamSchema.Fields.
    /// </summary>
    Object,

    /// <summary>
    /// Строковое значение из закрытого набора.
    /// Допустимые значения описываются через NodeParamSchema.EnumValues.
    /// </summary>
    Enum,

    /// <summary>
    /// Список структурных объектов с одинаковой схемой.
    /// Схема элемента описывается через NodeParamSchema.Fields.
    /// </summary>
    ObjectList,

    /// <summary>
    /// Двумерный массив структурных объектов с одинаковой схемой
    /// (массив рядов, каждый ряд — массив объектов).
    /// Схема одного объекта описывается через NodeParamSchema.Fields.
    /// </summary>
    ObjectMatrix,
}
