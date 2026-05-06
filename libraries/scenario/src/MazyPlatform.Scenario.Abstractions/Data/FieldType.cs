namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Тип поля пользовательской сущности.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// Строковое значение.
    /// </summary>
    String,

    /// <summary>
    /// Числовое значение.
    /// </summary>
    Number,

    /// <summary>
    /// Логическое значение.
    /// </summary>
    Boolean,

    /// <summary>
    /// Значение из списка допустимых вариантов.
    /// </summary>
    Enum,

    /// <summary>
    /// Дата и время.
    /// </summary>
    DateTime,

    /// <summary>
    /// Ссылка на запись другой сущности.
    /// Значение поля — RecordId связанной записи.
    /// </summary>
    Reference,
}
