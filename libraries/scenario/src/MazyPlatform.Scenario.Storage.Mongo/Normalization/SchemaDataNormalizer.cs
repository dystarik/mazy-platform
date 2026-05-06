namespace MazyPlatform.Scenario.Storage.Mongo.Normalization;

using System.Globalization;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Storage.Mongo.Documents;
using MazyPlatform.Scenario.Storage.Mongo.Exceptions;

internal static class SchemaDataNormalizer
{
    public static Dictionary<string, object?> ValidateAndNormalizeData(
        EntitySchemaDocument schema,
        IReadOnlyDictionary<string, object?> data,
        bool requireAllFields)
    {
        var normalized = new Dictionary<string, object?>(data, StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var field in schema.Fields)
        {
            if (!normalized.TryGetValue(field.Name, out var value) || value is null)
            {
                if (!string.IsNullOrWhiteSpace(field.DefaultValue))
                {
                    normalized[field.Name] = ParseDefaultValue(field);
                    continue;
                }

                if (requireAllFields && field.IsRequired)
                {
                    errors.Add($"Поле '{field.Name}' обязательно.");
                }

                continue;
            }

            if (!TryNormalizeValue(field, value, out var normalizedValue, out var error))
            {
                errors.Add(error);
                continue;
            }

            normalized[field.Name] = normalizedValue;
        }

        if (errors.Count > 0)
        {
            throw new DataValidationException(errors);
        }

        return normalized;
    }

    public static IReadOnlyDictionary<string, object?>? NormalizeFilter(
        EntitySchemaDocument schema,
        IReadOnlyDictionary<string, object?>? filter)
    {
        if (filter is null)
        {
            return null;
        }

        var normalized = new Dictionary<string, object?>(filter, StringComparer.Ordinal);
        var fields = schema.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
        var errors = new List<string>();

        foreach (var (key, value) in filter)
        {
            if (value is null || !fields.TryGetValue(key, out var field))
            {
                continue;
            }

            if (!TryNormalizeValue(field, value, out var normalizedValue, out var error))
            {
                errors.Add(error);
                continue;
            }

            normalized[key] = normalizedValue;
        }

        if (errors.Count > 0)
        {
            throw new DataValidationException(errors);
        }

        return normalized;
    }

    private static object? ParseDefaultValue(EntitySchemaFieldDocument field)
    {
        if (field.DefaultValue is null)
        {
            return null;
        }

        return TryNormalizeValue(field, field.DefaultValue, out var normalized, out _)
            ? normalized
            : field.DefaultValue;
    }

    private static bool TryNormalizeValue(
        EntitySchemaFieldDocument field,
        object value,
        out object normalized,
        out string error)
    {
        normalized = value;
        error = string.Empty;

        if (string.Equals(field.Type, nameof(FieldType.String), StringComparison.Ordinal) && value is string)
        {
            return true;
        }

        if (string.Equals(field.Type, nameof(FieldType.Number), StringComparison.Ordinal)
            && TryNormalizeNumber(value, out normalized))
        {
            return true;
        }

        if (string.Equals(field.Type, nameof(FieldType.Boolean), StringComparison.Ordinal)
            && TryNormalizeBoolean(value, out normalized))
        {
            return true;
        }

        if (string.Equals(field.Type, nameof(FieldType.DateTime), StringComparison.Ordinal)
            && TryNormalizeDateTime(value, out normalized))
        {
            return true;
        }

        if (string.Equals(field.Type, nameof(FieldType.Reference), StringComparison.Ordinal)
            && TryNormalizeGuid(value, out normalized))
        {
            return true;
        }

        if (string.Equals(field.Type, nameof(FieldType.Enum), StringComparison.Ordinal)
            && TryNormalizeEnum(field, value, out normalized))
        {
            return true;
        }

        error = $"Поле '{field.Name}' имеет неверный тип для '{field.Type}'.";
        return false;
    }

    private static bool TryNormalizeNumber(object value, out object normalized)
    {
        normalized = value;

        if (IsNumber(value))
        {
            return true;
        }

        if (value is not string stringValue)
        {
            return false;
        }

        var trimmed = stringValue.Trim();

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            normalized = intValue;
            return true;
        }

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            normalized = longValue;
            return true;
        }

        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            && double.IsFinite(doubleValue))
        {
            normalized = doubleValue;
            return true;
        }

        return false;
    }

    private static bool TryNormalizeBoolean(object value, out object normalized)
    {
        normalized = value;

        if (value is bool)
        {
            return true;
        }

        if (value is string stringValue && bool.TryParse(stringValue.Trim(), out var parsed))
        {
            normalized = parsed;
            return true;
        }

        return false;
    }

    private static bool TryNormalizeEnum(EntitySchemaFieldDocument field, object value, out object normalized)
    {
        normalized = value;

        if (value is not string stringValue)
        {
            return false;
        }

        if (field.EnumValues?.Contains(stringValue, StringComparer.Ordinal) != true)
        {
            return false;
        }

        normalized = stringValue;
        return true;
    }

    private static bool TryNormalizeDateTime(object value, out object normalized)
    {
        if (value is DateTime dateTime)
        {
            normalized = dateTime;
            return true;
        }

        if (value is string stringValue
            && DateTime.TryParse(
                stringValue.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            normalized = parsed;
            return true;
        }

        normalized = value;
        return false;
    }

    private static bool TryNormalizeGuid(object value, out object normalized)
    {
        if (value is Guid guid)
        {
            normalized = guid;
            return true;
        }

        if (value is string stringValue && Guid.TryParse(stringValue.Trim(), out var parsed))
        {
            normalized = parsed;
            return true;
        }

        normalized = value;
        return false;
    }

    private static bool IsNumber(object value) => value is int or long or float or double or decimal;
}
