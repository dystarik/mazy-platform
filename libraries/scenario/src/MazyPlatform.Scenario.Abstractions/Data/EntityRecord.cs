namespace MazyPlatform.Scenario.Abstractions.Data;

/// <summary>
/// Запись пользовательской сущности (экземпляр данных).
/// </summary>
/// <param name="RecordId">Идентификатор записи.</param>
/// <param name="SchemaId">Идентификатор схемы.</param>
/// <param name="Data">Данные в формате ключ-значение.</param>
/// <param name="SessionId">Привязка к сессии диалога (опционально).</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="UpdatedAt">Дата обновления.</param>
public sealed record EntityRecord(
    Guid RecordId,
    Guid SchemaId,
    IReadOnlyDictionary<string, object?> Data,
    Guid? SessionId = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null)
{
    /// <summary>
    /// Идентификатор snapshot-а схемы, по которому запись была создана.
    /// </summary>
    public Guid SchemaSnapshotId { get; init; } = SchemaId;

    /// <summary>
    /// Идентификатор проекта.
    /// </summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Версия сценария, по схеме которой создана запись.
    /// </summary>
    public int? ScenarioVersion { get; init; }

    /// <summary>
    /// Идентификатор экземпляра бота.
    /// </summary>
    public Guid? BotId { get; init; }

    /// <summary>
    /// Идентификатор пользователя на платформе.
    /// </summary>
    public string? PlatformUserId { get; init; }

    /// <summary>
    /// Признак legacy-записи, которую нельзя безопасно привязать к пользователю.
    /// </summary>
    public bool IsArchived { get; init; }
}
