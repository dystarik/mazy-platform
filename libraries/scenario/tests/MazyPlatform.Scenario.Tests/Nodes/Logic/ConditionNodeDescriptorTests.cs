namespace MazyPlatform.Scenario.Tests.Nodes.Logic;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ConditionNodeDescriptorTests
{
    [Test]
    public async Task Schema_HasThreeFieldsAndNotExpression()
    {
        var descriptor = new ConditionNodeDescriptor();
        var keys = descriptor.Schema.Select(p => p.Key).ToList();

        await Assert.That(keys).Contains("leftOperand");
        await Assert.That(keys).Contains("operator");
        await Assert.That(keys).Contains("rightOperand");
        await Assert.That(keys).DoesNotContain("expression");
    }

    [Test]
    public async Task Schema_OperatorParameter_IsEnumWithEightValues()
    {
        var descriptor = new ConditionNodeDescriptor();

        var op = descriptor.Schema.Single(
            p => string.Equals(p.Key, "operator", StringComparison.Ordinal));

        await Assert.That(op.Type).IsEqualTo(NodeParamType.Enum);
        await Assert.That(op.IsRequired).IsTrue();
        await Assert.That(op.EnumValues).IsNotNull();
        await Assert.That(op.EnumValues!.Count).IsEqualTo(8);
        foreach (var expected in new[] { "==", "!=", ">", "<", ">=", "<=", "contains", "startsWith" })
        {
            await Assert.That(op.EnumValues).Contains(expected);
        }
    }

    [Test]
    public async Task Validate_WithValidParameters_ReturnsNoErrors()
    {
        var descriptor = new ConditionNodeDescriptor();
        var parameters = ParseJson("""
            { "leftOperand": "{status}", "operator": "==", "rightOperand": "new" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithUnknownOperator_ReturnsEnumError()
    {
        var descriptor = new ConditionNodeDescriptor();
        var parameters = ParseJson("""
            { "leftOperand": "a", "operator": "===", "rightOperand": "b" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("operator");
    }

    [Test]
    public async Task Validate_MissingLeftOperand_ReturnsRequiredError()
    {
        var descriptor = new ConditionNodeDescriptor();
        var parameters = ParseJson("""
            { "operator": "==", "rightOperand": "b" }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("leftOperand");
    }

    [Test]
    [Arguments("==", "yes", "yes", "true")]
    [Arguments("==", "yes", "no", "false")]
    [Arguments("!=", "yes", "no", "true")]
    [Arguments("!=", "yes", "yes", "false")]
    [Arguments(">", "10", "5", "true")]
    [Arguments(">", "5", "10", "false")]
    [Arguments("<", "5", "10", "true")]
    [Arguments(">=", "5", "5", "true")]
    [Arguments("<=", "4", "3", "false")]
    [Arguments("contains", "привет мир", "мир", "true")]
    [Arguments("contains", "привет", "пока", "false")]
    [Arguments("startsWith", "hello world", "hello", "true")]
    [Arguments("startsWith", "hello", "world", "false")]
    public async Task CreateAndExecute_AllOperators_BranchAsExpected(
        string op,
        string left,
        string right,
        string expectedBranch)
    {
        var descriptor = new ConditionNodeDescriptor();
        var parameters = ParseJson($$"""
            { "leftOperand": "{{left}}", "operator": "{{op}}", "rightOperand": "{{right}}" }
            """);
        var services = Substitute.For<IServiceProvider>();

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);
        var result = await node.ExecuteAsync(CreateContext());

        await Assert.That(result.BranchKey).IsEqualTo(expectedBranch);
    }

    [Test]
    public async Task CreateAndExecute_ResolvesVariablesInBothOperands()
    {
        var descriptor = new ConditionNodeDescriptor();
        var parameters = ParseJson("""
            { "leftOperand": "{count}", "operator": ">=", "rightOperand": "{threshold}" }
            """);
        var services = Substitute.For<IServiceProvider>();

        var node = descriptor.Create(Guid.NewGuid(), parameters, services);
        var context = CreateContext(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["count"] = "100",
            ["threshold"] = "50",
        });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.BranchKey).IsEqualTo("true");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

    private static ExecutionContext CreateContext(IDictionary<string, object?>? variables = null)
    {
        var session = new TestSession();
        if (variables is not null)
        {
            foreach (var (key, value) in variables)
            {
                session.Variables[key] = value;
            }
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
