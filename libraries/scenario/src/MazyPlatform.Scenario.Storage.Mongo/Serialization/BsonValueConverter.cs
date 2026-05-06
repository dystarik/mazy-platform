namespace MazyPlatform.Scenario.Storage.Mongo.Serialization;

using System.Text.Json;

using MongoDB.Bson;

internal static class BsonValueConverter
{
    public static BsonDocument ToBsonDocument(IEnumerable<KeyValuePair<string, object?>> source)
    {
        var document = new BsonDocument();

        foreach (var item in source)
        {
            document[item.Key] = ToBsonValue(item.Value);
        }

        return document;
    }

    public static Dictionary<string, object?> ToDictionary(BsonDocument source)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var element in source.Elements)
        {
            dictionary[element.Name] = ToObjectValue(element.Value);
        }

        return dictionary;
    }

    public static BsonValue ToBsonValue(object? value)
    {
        return value switch
        {
            null => BsonNull.Value,
            string stringValue => new BsonString(stringValue),
            int intValue => new BsonInt32(intValue),
            long longValue => new BsonInt64(longValue),
            float floatValue => new BsonDouble(floatValue),
            double doubleValue => new BsonDouble(doubleValue),
            decimal decimalValue => new BsonDecimal128(decimalValue),
            bool boolValue => new BsonBoolean(boolValue),
            Guid guidValue => new BsonString(guidValue.ToString()),
            DateTime dateTimeValue => new BsonDateTime(dateTimeValue.ToUniversalTime()),
            JsonElement jsonElement => ConvertJsonElement(jsonElement),
            IReadOnlyDictionary<string, object?> readOnlyDictionary => ToBsonDocument(readOnlyDictionary),
            IDictionary<string, object?> dictionary => ToBsonDocument(dictionary),
            IEnumerable<object?> objectEnumerable => new BsonArray(objectEnumerable.Select(ToBsonValue)),
            System.Collections.IEnumerable enumerable when value is not string => new BsonArray(enumerable.Cast<object?>().Select(ToBsonValue)),
            _ => BsonValue.Create(value),
        };
    }

    public static object? ToObjectValue(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Document => ToDictionary(value.AsBsonDocument),
            BsonType.Array => value.AsBsonArray.Select(ToObjectValue).ToList(),
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Decimal128 => Decimal128.ToDecimal(value.AsDecimal128),
            BsonType.Double => value.AsDouble,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Null => null,
            BsonType.String => value.AsString,
            BsonType.Binary when value.IsGuid => value.AsGuid,
            _ => value.ToString(),
        };
    }

    private static BsonValue ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => new BsonArray(element.EnumerateArray().Select(ConvertJsonElement)),
            JsonValueKind.String when element.TryGetDateTime(out var dateTime) => new BsonDateTime(dateTime.ToUniversalTime()),
            JsonValueKind.String => new BsonString(element.GetString() ?? string.Empty),
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => new BsonInt32(intValue),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => new BsonInt64(longValue),
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => new BsonDecimal128(decimalValue),
            JsonValueKind.Number => new BsonDouble(element.GetDouble()),
            JsonValueKind.True => BsonBoolean.True,
            JsonValueKind.False => BsonBoolean.False,
            _ => BsonNull.Value,
        };
    }

    private static BsonDocument ConvertJsonObject(JsonElement element)
    {
        var document = new BsonDocument();

        foreach (var property in element.EnumerateObject())
        {
            document[property.Name] = ConvertJsonElement(property.Value);
        }

        return document;
    }
}
