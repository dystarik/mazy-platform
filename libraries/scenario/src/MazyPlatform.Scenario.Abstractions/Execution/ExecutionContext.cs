namespace MazyPlatform.Scenario.Abstractions.Execution;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Контекст выполнения сценария.
/// Передаётся в каждый узел при выполнении.
/// </summary>
public sealed class ExecutionContext
{
    /// <summary>
    /// Текущая сессия диалога.
    /// </summary>
    public required ISession Session { get; init; }

    /// <summary>
    /// Входящее событие, вызвавшее текущий цикл выполнения.
    /// Null при первом запуске без события.
    /// </summary>
    public IIncomingEvent? IncomingEvent { get; init; }

    /// <summary>
    /// Хранилище пользовательских данных.
    /// </summary>
    public required IDataStore DataStore { get; init; }

    /// <summary>
    /// Хранилище схем сущностей.
    /// </summary>
    public required ISchemaStore SchemaStore { get; init; }

    /// <summary>
    /// Идентификатор проекта (для доступа к данным и схемам).
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// Версия сценария, которая сейчас выполняется.
    /// </summary>
    public int ScenarioVersion { get; init; }

    /// <summary>
    /// Токен бота для текущего запроса.
    /// </summary>
    public required string BotToken { get; init; }

    /// <summary>
    /// Область видимости пользовательских данных для текущего выполнения.
    /// </summary>
    public DataScope DataScope => new(
        ProjectId,
        ScenarioVersion,
        Session.BotId,
        Session.PlatformUserId);

    /// <summary>
    /// Подставляет переменные из сессии в текст.
    /// "{client_name}" → значение из Session.Variables["client_name"].
    /// </summary>
    /// <param name="template">Шаблон с переменными в фигурных скобках.</param>
    /// <returns>Текст с подставленными значениями.</returns>
    public string ResolveVariables(string template)
    {
        var result = template;

        foreach (var (key, value) in Session.Variables)
        {
            result = result.Replace(
                $"{{{key}}}",
                value?.ToString() ?? string.Empty,
                StringComparison.Ordinal);
        }

        return result;
    }
}
