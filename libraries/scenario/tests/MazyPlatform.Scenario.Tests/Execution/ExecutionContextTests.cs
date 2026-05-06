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
    public async Task ResolveVariables_WithoutVariables_ReturnsOriginalText()
    {
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal));

        var result = context.ResolveVariables("Обычный текст");

        await Assert.That(result).IsEqualTo("Обычный текст");
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
