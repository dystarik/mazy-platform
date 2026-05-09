namespace MazyPlatform.Scenario.Tests.Execution;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ExecutionContextTests
{
    [Test]
    public async Task ResolveVariables_ReplacesSingleVariable()
    {
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" });

        var result = context.ResolveVariables("{name}");

        await Assert.That(result).IsEqualTo("Иван");
    }

    [Test]
    public async Task ResolveVariables_ReplacesMultipleVariables()
    {
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = "Иван",
            ["phone"] = "+79991234567",
        });

        var result = context.ResolveVariables("Привет, {name}! Ваш телефон: {phone}");

        await Assert.That(result).IsEqualTo("Привет, Иван! Ваш телефон: +79991234567");
    }

    [Test]
    public async Task ResolveVariables_UnknownVariable_RemainsUnchanged()
    {
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal));

        var result = context.ResolveVariables("{unknown}");

        await Assert.That(result).IsEqualTo("{unknown}");
    }

    [Test]
    public async Task ResolveVariables_NullValue_ReplacedWithEmptyString()
    {
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = null });

        var result = context.ResolveVariables("Привет, {name}!");

        await Assert.That(result).IsEqualTo("Привет, !");
    }

    [Test]
    public async Task ResolveVariables_DictionaryPath_ReplacesWithNestedField()
    {
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["record"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["phone"] = "+79991234567",
                },
            });

        var result = context.ResolveVariables("Телефон: {record.phone}");

        await Assert.That(result).IsEqualTo("Телефон: +79991234567");
    }

    [Test]
    public async Task ResolveVariables_ListPath_ReplacesWithIndexedNestedField()
    {
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["records"] = new List<object?>
                {
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["name"] = "Первый",
                    },
                },
            });

        var result = context.ResolveVariables("Запись: {records.0.name}");

        await Assert.That(result).IsEqualTo("Запись: Первый");
    }

    [Test]
    public async Task ResolveVariables_WithoutVariables_ReturnsOriginalText()
    {
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal));

        var result = context.ResolveVariables("Обычный текст");

        await Assert.That(result).IsEqualTo("Обычный текст");
    }

    [Test]
    public async Task ResolveVariables_DictionaryValue_ReplacesWithReadableFields()
    {
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["record"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = "Иван",
                    ["age"] = 30,
                },
            });

        var result = context.ResolveVariables("{record}");

        await Assert.That(result).IsEqualTo($"name: Иван{Environment.NewLine}age: 30");
    }

    [Test]
    public async Task ResolveVariables_DictionaryField_ReplacesWithFieldValue()
    {
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["record"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = "Иван",
                },
            });

        var result = context.ResolveVariables("Клиент: {record.name}");

        await Assert.That(result).IsEqualTo("Клиент: Иван");
    }

    [Test]
    public async Task ResolveVariables_ListOfDictionaries_ReplacesWithReadableFields()
    {
        var records = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" },
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Мария" },
        };
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["records"] = records });

        var result = context.ResolveVariables("{records}");

        await Assert.That(result).IsEqualTo($"name: Иван{Environment.NewLine}name: Мария");
    }

    [Test]
    public async Task ResolveVariables_EntityRecord_ReplacesWithReadableFields()
    {
        var record = new EntityRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Иван" });
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["record"] = record });

        var result = context.ResolveVariables("{record}");

        await Assert.That(result).IsEqualTo("name: Иван");
    }

    private static ExecutionContext CreateContext(IDictionary<string, object?> variables)
    {
        var session = new TestSession();

        foreach (var (key, value) in variables)
        {
            session.Variables[key] = value;
        }

        return new ExecutionContext
        {
            Session = session,
            DataStore = Substitute.For<IDataStore>(),
            SchemaStore = Substitute.For<ISchemaStore>(),
            BotToken = string.Empty,
            ProjectId = Guid.NewGuid(),
        };
    }
}
