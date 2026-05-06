namespace MazyPlatform.Scenario.Helpers;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Actions;

/// <summary>
/// Вспомогательные методы для извлечения параметров узлов из JSON.
/// </summary>
public static class JsonParamHelper
{
    /// <summary>
    /// Извлекает обязательный строковый параметр.
    /// </summary>
    /// <param name="element">JSON-элемент параметров.</param>
    /// <param name="property">Имя свойства.</param>
    /// <returns>Значение свойства.</returns>
    public static string GetRequiredString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            throw new InvalidOperationException($"Обязательный параметр \"{property}\" не найден.");
        }

        return value.GetString()
            ?? throw new InvalidOperationException($"Параметр \"{property}\" не может быть null.");
    }

    /// <summary>
    /// Извлекает необязательный строковый параметр.
    /// </summary>
    /// <param name="element">JSON-элемент параметров.</param>
    /// <param name="property">Имя свойства.</param>
    /// <returns>Значение свойства или null.</returns>
    public static string? GetOptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() : null;

    /// <summary>
    /// Извлекает необязательный булевый параметр.
    /// </summary>
    /// <param name="element">JSON-элемент параметров.</param>
    /// <param name="property">Имя свойства.</param>
    /// <returns>Значение свойства или false.</returns>
    public static bool GetOptionalBool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.GetBoolean();

    /// <summary>
    /// Извлекает необязательный список строк.
    /// </summary>
    /// <param name="element">JSON-элемент параметров.</param>
    /// <param name="property">Имя свойства.</param>
    /// <returns>Список строк или null.</returns>
    public static IReadOnlyList<string>? GetOptionalStringList(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return [.. value.EnumerateArray().Select(e => e.GetString()!)];
    }

    /// <summary>
    /// Извлекает обязательный строковый словарь.
    /// </summary>
    /// <param name="element">JSON-элемент параметров.</param>
    /// <param name="property">Имя свойства.</param>
    /// <returns>Словарь строк.</returns>
    public static IReadOnlyDictionary<string, string> GetRequiredStringDictionary(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Параметр \"{property}\" должен быть объектом.");
        }

        return value.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    /// <summary>
    /// Извлекает необязательный строковый словарь.
    /// </summary>
    /// <param name="element">JSON-элемент параметров.</param>
    /// <param name="property">Имя свойства.</param>
    /// <returns>Словарь строк или null.</returns>
    public static IReadOnlyDictionary<string, string>? GetOptionalStringDictionary(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return value.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    /// <summary>
    /// Парсит двумерный массив описаний кнопок из JSON по ключу <c>"buttons"</c>.
    /// Если ключ отсутствует — возвращает пустую раскладку.
    /// </summary>
    /// <param name="parameters">JSON-элемент параметров узла.</param>
    /// <returns>Раскладка кнопок (может быть пустой).</returns>
    public static ButtonLayout ParseButtonLayout(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("buttons", out var buttonsElement)
            || buttonsElement.ValueKind != JsonValueKind.Array)
        {
            return ButtonLayout.Empty;
        }

        return new ButtonLayout(
            [.. buttonsElement.EnumerateArray()
                .Select(ParseButtonRow)]);
    }

    /// <summary>
    /// Парсит массив кейсов Switch из JSON по ключу <c>"cases"</c>.
    /// Преобразует <c>[{value, branchKey}, ...]</c> в словарь
    /// для быстрого поиска ветки по значению переменной в рантайме.
    /// </summary>
    /// <param name="parameters">JSON-элемент параметров узла.</param>
    /// <returns>Словарь <c>value → branchKey</c>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Если в массиве встречаются два элемента с одинаковым <c>value</c>.
    /// </exception>
    public static IReadOnlyDictionary<string, string> ParseCases(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("cases", out var casesElement)
            || casesElement.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var cases = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in casesElement.EnumerateArray())
        {
            var value = item.GetProperty("value").GetString() ?? string.Empty;
            var branchKey = item.GetProperty("branchKey").GetString() ?? string.Empty;

            if (!cases.TryAdd(value, branchKey))
            {
                throw new InvalidOperationException(
                    $"Дублирующееся значение \"{value}\" в cases — в одном Switch-узле "
                    + "не может быть двух веток с одинаковым значением для сравнения.");
            }
        }

        return cases;
    }

    /// <summary>
    /// Парсит опциональный двумерный массив описаний кнопок из JSON по ключу <c>"buttons"</c>.
    /// Возвращает <c>null</c>, если ключ отсутствует — это сигнализирует «не менять кнопки»
    /// (для узлов вроде edit_message, где отсутствие поля и пустой массив имеют разный смысл).
    /// </summary>
    /// <param name="parameters">JSON-элемент параметров узла.</param>
    /// <returns>Раскладка кнопок или <c>null</c>, если поле отсутствует.</returns>
    public static ButtonLayout? ParseOptionalButtonLayout(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("buttons", out var buttonsElement)
            || buttonsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return new ButtonLayout(
            [.. buttonsElement.EnumerateArray()
                .Select(ParseButtonRow)]);
    }

    private static IReadOnlyList<ButtonDefinition> ParseButtonRow(JsonElement row) =>
        [.. row.EnumerateArray().Select(ParseButton)];

    private static ButtonDefinition ParseButton(JsonElement button) =>
        new(
            button.GetProperty("label").GetString() ?? string.Empty,
            button.GetProperty("payload").GetString() ?? string.Empty,
            ParseButtonStyle(button));

    private static ButtonStyle? ParseButtonStyle(JsonElement button)
    {
        if (!button.TryGetProperty("style", out var styleElement)
            || styleElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return styleElement.GetString() switch
        {
            "primary" => ButtonStyle.Primary,
            "secondary" => ButtonStyle.Secondary,
            "success" => ButtonStyle.Success,
            "danger" => ButtonStyle.Danger,
            _ => null,
        };
    }
}
