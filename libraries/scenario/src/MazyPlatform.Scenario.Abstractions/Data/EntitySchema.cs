namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Схема пользовательской сущности (создаётся пользователем платформы).
/// Например: "Заявка" с полями "имя", "телефон", "статус".
/// </summary>
/// <param name="SchemaSnapshotId">Идентификатор snapshot-а схемы для конкретной версии сценария.</param>
/// <param name="SchemaId">Стабильный идентификатор схемы.</param>
/// <param name="ProjectId">Идентификатор проекта.</param>
/// <param name="ScenarioVersion">Версия сценария, к которой относится snapshot.</param>
/// <param name="Name">Название сущности.</param>
/// <param name="Fields">Описание полей.</param>
public sealed record EntitySchema(
    Guid SchemaSnapshotId,
    Guid SchemaId,
    Guid ProjectId,
    int ScenarioVersion,
    string Name,
    IReadOnlyList<EntityField> Fields)
{
    /// <summary>
    /// Создаёт legacy-схему без версии. Используется для обратной совместимости.
    /// </summary>
    /// <param name="schemaId">Идентификатор схемы.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="name">Название сущности.</param>
    /// <param name="fields">Описание полей.</param>
    public EntitySchema(
        Guid schemaId,
        Guid projectId,
        string name,
        IReadOnlyList<EntityField> fields)
        : this(schemaId, schemaId, projectId, 0, name, fields)
    {
    }
}
