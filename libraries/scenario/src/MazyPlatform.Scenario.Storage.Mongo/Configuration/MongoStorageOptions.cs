namespace MazyPlatform.Scenario.Storage.Mongo.Configuration;

/// <summary>
/// Настройки MongoDB-хранилищ сценариев.
/// </summary>
public sealed class MongoStorageOptions
{
    /// <summary>
    /// Строка подключения к MongoDB.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Имя базы данных.
    /// </summary>
    public required string DatabaseName { get; set; }

    /// <summary>
    /// Время жизни сессии в часах.
    /// </summary>
    public int SessionTtlHours { get; set; } = 24;
}
