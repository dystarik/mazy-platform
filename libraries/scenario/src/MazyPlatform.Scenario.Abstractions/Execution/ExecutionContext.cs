namespace MazyPlatform.Scenario.Abstractions.Execution;

using System.Collections;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Events;
using MazyPlatform.Scenario.Abstractions.Sessions;

/// <summary>
/// Контекст выполнения сценария.
/// Передаётся в каждый узел при выполнении.
/// </summary>
public sealed partial class ExecutionContext
{
    private static readonly JsonSerializerOptions _variableJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

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

    [GeneratedRegex(@"\{(?<path>[^{}]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex VariablePattern { get; }

    /// <summary>
    /// Подставляет переменные из сессии в текст.
    /// "{client_name}" → значение из Session.Variables["client_name"].
    /// </summary>
    /// <param name="template">Шаблон с переменными в фигурных скобках.</param>
    /// <returns>Текст с подставленными значениями.</returns>
    public string ResolveVariables(string template)
    {
        return VariablePattern.Replace(
            template,
            match =>
            {
                var path = match.Groups["path"].Value;

                return TryResolveVariableValue(path, out var value)
                    ? FormatVariableValue(value)
                    : match.Value;
            });
    }

    private static bool TryGetNestedValue(object? source, string key, out object? value)
    {
        if (source is EntityRecord record)
            source = record.Data;

        switch (source)
        {
            case IReadOnlyDictionary<string, object?> readOnlyDictionary:
                return readOnlyDictionary.TryGetValue(key, out value);
            case IDictionary<string, object?> dictionary:
                return dictionary.TryGetValue(key, out value);
            default:
                value = null;
                return false;
        }
    }

    private static string FormatVariableValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string stringValue => stringValue,
            IFormattable formattableValue => formattableValue.ToString(null, CultureInfo.InvariantCulture),
            EntityRecord record => JsonSerializer.Serialize(record.Data, _variableJsonOptions),
            IReadOnlyDictionary<string, object?> or IDictionary<string, object?> => JsonSerializer.Serialize(value, _variableJsonOptions),
            IEnumerable enumerableValue => JsonSerializer.Serialize(enumerableValue, _variableJsonOptions),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private bool TryResolveVariableValue(string path, out object? value)
    {
        if (Session.Variables.TryGetValue(path, out value))
            return true;

        var pathParts = path.Split('.');

        if (pathParts.Length < 2 || !Session.Variables.TryGetValue(pathParts[0], out value))
            return false;

        for (var i = 1; i < pathParts.Length; i++)
        {
            if (!TryGetNestedValue(value, pathParts[i], out value))
                return false;
        }

        return true;
    }
}
