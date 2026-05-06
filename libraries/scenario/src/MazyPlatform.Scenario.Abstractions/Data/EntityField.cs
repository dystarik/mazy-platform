namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Описание поля в схеме пользовательской сущности.
/// </summary>
/// <param name="Name">Имя поля (например, "client_name").</param>
/// <param name="Type">Тип данных.</param>
/// <param name="IsRequired">Обязательность заполнения.</param>
/// <param name="DefaultValue">Значение по умолчанию.</param>
/// <param name="EnumValues">Допустимые значения для типа Enum.</param>
/// <param name="ReferenceEntityName">
/// Имя сущности, на которую ссылается поле.
/// Используется только для типа Reference.
/// </param>
public sealed record EntityField(
    string Name,
    FieldType Type,
    bool IsRequired = false,
    string? DefaultValue = null,
    IReadOnlyList<string>? EnumValues = null,
    string? ReferenceEntityName = null);
