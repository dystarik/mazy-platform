namespace MazyPlatform.Scenario.Tests.Storage.Mongo;

using System.Globalization;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Storage.Mongo.Documents;
using MazyPlatform.Scenario.Storage.Mongo.Exceptions;
using MazyPlatform.Scenario.Storage.Mongo.Normalization;

public sealed class SchemaDataNormalizerTests
{
    [Test]
    public async Task ValidateAndNormalizeData_NumberStrings_NormalizesToNumericTypes()
    {
        var schema = CreateSchema(
            Field("int_value", FieldType.Number),
            Field("long_value", FieldType.Number),
            Field("double_value", FieldType.Number));

        var normalized = SchemaDataNormalizer.ValidateAndNormalizeData(
            schema,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["int_value"] = "42",
                ["long_value"] = "9223372036854775807",
                ["double_value"] = "12.5",
            },
            requireAllFields: true);

        await Assert.That(normalized["int_value"]).IsTypeOf<int>();
        await Assert.That(normalized["int_value"]).IsEqualTo(42);
        await Assert.That(normalized["long_value"]).IsTypeOf<long>();
        await Assert.That(normalized["long_value"]).IsEqualTo(9223372036854775807L);
        await Assert.That(normalized["double_value"]).IsTypeOf<double>();
        await Assert.That(normalized["double_value"]).IsEqualTo(12.5);
    }

    [Test]
    public async Task ValidateAndNormalizeData_InvalidNumberStrings_ThrowsDataValidationException()
    {
        var schema = CreateSchema(
            Field("comma_value", FieldType.Number),
            Field("text_value", FieldType.Number));

        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["comma_value"] = "12,5",
            ["text_value"] = "abc",
        };

        await Assert
            .That(() => SchemaDataNormalizer.ValidateAndNormalizeData(schema, data, requireAllFields: true))
            .ThrowsExactly<DataValidationException>();
    }

    [Test]
    public async Task ValidateAndNormalizeData_BooleanStrings_NormalizesBoolValues()
    {
        var schema = CreateSchema(
            Field("enabled", FieldType.Boolean),
            Field("archived", FieldType.Boolean));

        var normalized = SchemaDataNormalizer.ValidateAndNormalizeData(
            schema,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["enabled"] = " true ",
                ["archived"] = "false",
            },
            requireAllFields: true);

        await Assert.That((bool)normalized["enabled"]!).IsTrue();
        await Assert.That((bool)normalized["archived"]!).IsFalse();
    }

    [Test]
    public async Task ValidateAndNormalizeData_InvalidBooleanString_ThrowsDataValidationException()
    {
        var schema = CreateSchema(Field("enabled", FieldType.Boolean));
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["enabled"] = "1",
        };

        await Assert
            .That(() => SchemaDataNormalizer.ValidateAndNormalizeData(schema, data, requireAllFields: true))
            .ThrowsExactly<DataValidationException>();
    }

    [Test]
    public async Task ValidateAndNormalizeData_DateTimeAndReferenceStrings_TrimsAndNormalizes()
    {
        var recordId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow;
        var schema = CreateSchema(
            Field("created_at", FieldType.DateTime),
            Field("owner", FieldType.Reference));

        var normalized = SchemaDataNormalizer.ValidateAndNormalizeData(
            schema,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["created_at"] = $" {dateTime.ToString("O", CultureInfo.InvariantCulture)} ",
                ["owner"] = $" {recordId} ",
            },
            requireAllFields: true);

        await Assert.That(normalized["created_at"]).IsTypeOf<DateTime>();
        await Assert.That(normalized["created_at"]).IsEqualTo(dateTime);
        await Assert.That(normalized["owner"]).IsTypeOf<Guid>();
        await Assert.That(normalized["owner"]).IsEqualTo(recordId);
    }

    [Test]
    public async Task ValidateAndNormalizeData_EnumRequiresExactValue()
    {
        var schema = CreateSchema(Field("status", FieldType.Enum, ["new", "done"]));

        var normalized = SchemaDataNormalizer.ValidateAndNormalizeData(
            schema,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["status"] = "new",
            },
            requireAllFields: true);

        await Assert.That(normalized["status"]).IsEqualTo("new");

        await Assert
            .That(() => SchemaDataNormalizer.ValidateAndNormalizeData(
                schema,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["status"] = " new ",
                },
                requireAllFields: true))
            .ThrowsExactly<DataValidationException>();
    }

    [Test]
    public async Task ValidateAndNormalizeData_StringAndUnknownFields_ArePreserved()
    {
        var schema = CreateSchema(Field("comment", FieldType.String));

        var normalized = SchemaDataNormalizer.ValidateAndNormalizeData(
            schema,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["comment"] = "  keep spaces  ",
                ["unknown_number"] = "42",
            },
            requireAllFields: true);

        await Assert.That(normalized["comment"]).IsEqualTo("  keep spaces  ");
        await Assert.That(normalized["unknown_number"]).IsTypeOf<string>();
        await Assert.That(normalized["unknown_number"]).IsEqualTo("42");
    }

    [Test]
    public async Task ValidateAndNormalizeData_UpdateMode_DoesNotRequireMissingRequiredFields()
    {
        var schema = CreateSchema(
            Field("name", FieldType.String, isRequired: true),
            Field("age", FieldType.Number));

        var normalized = SchemaDataNormalizer.ValidateAndNormalizeData(
            schema,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["age"] = "42",
            },
            requireAllFields: false);

        await Assert.That(normalized.ContainsKey("name")).IsFalse();
        await Assert.That(normalized["age"]).IsEqualTo(42);
    }

    [Test]
    public async Task NormalizeFilter_UsesSameFieldNormalizationAsData()
    {
        var schema = CreateSchema(
            Field("age", FieldType.Number),
            Field("enabled", FieldType.Boolean),
            Field("unknown", FieldType.String));
        var filter = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["age"] = "42",
            ["enabled"] = " true ",
            ["external"] = "12.5",
        };

        var normalized = SchemaDataNormalizer.NormalizeFilter(schema, filter)!;

        await Assert.That(normalized["age"]).IsTypeOf<int>();
        await Assert.That(normalized["age"]).IsEqualTo(42);
        await Assert.That((bool)normalized["enabled"]!).IsTrue();
        await Assert.That(normalized["external"]).IsTypeOf<string>();
        await Assert.That(normalized["external"]).IsEqualTo("12.5");
    }

    [Test]
    public async Task NormalizeFilter_InvalidKnownField_ThrowsDataValidationException()
    {
        var schema = CreateSchema(Field("age", FieldType.Number));
        var filter = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["age"] = "abc",
        };

        await Assert
            .That(() => SchemaDataNormalizer.NormalizeFilter(schema, filter))
            .ThrowsExactly<DataValidationException>();
    }

    private static EntitySchemaDocument CreateSchema(params EntitySchemaFieldDocument[] fields) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchemaId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ScenarioVersion = 1,
            Name = "Заявка",
            Fields = fields,
        };

    private static EntitySchemaFieldDocument Field(
        string name,
        FieldType type,
        IReadOnlyList<string>? enumValues = null,
        bool isRequired = false) =>
        new()
        {
            Name = name,
            Type = type.ToString(),
            IsRequired = isRequired,
            EnumValues = enumValues,
        };
}
