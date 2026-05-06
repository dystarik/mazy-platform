namespace MazyPlatform.Scenario.Tests.Nodes.Logic;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Logic;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class SwitchNodeDescriptorTests
{
    [Test]
    public async Task Schema_CasesParameter_IsObjectList()
    {
        var descriptor = new SwitchNodeDescriptor();

        var cases = descriptor.Schema.Single(
            p => string.Equals(p.Key, "cases", StringComparison.Ordinal));

        await Assert.That(cases.Type).IsEqualTo(NodeParamType.ObjectList);
        await Assert.That(cases.Fields).IsNotNull();
        await Assert.That(cases.Fields!.Select(f => f.Key).ToList()).Contains("value");
        await Assert.That(cases.Fields!.Select(f => f.Key).ToList()).Contains("branchKey");
    }

    [Test]
    public async Task Validate_WithValidCases_ReturnsNoErrors()
    {
        var descriptor = new SwitchNodeDescriptor();
        var parameters = ParseJson("""
            {
              "variable": "status",
              "cases": [
                {"value": "new", "branchKey": "case_new"},
                {"value": "done", "branchKey": "case_done"}
              ]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_CaseMissingValue_ReturnsErrorWithPath()
    {
        var descriptor = new SwitchNodeDescriptor();
        var parameters = ParseJson("""
            {
              "variable": "status",
              "cases": [{"branchKey": "case_new"}]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"cases[0]\"");
        await Assert.That(errors[0]).Contains("\"value\"");
    }

    [Test]
    public async Task Validate_CaseMissingBranchKey_ReturnsErrorWithPath()
    {
        var descriptor = new SwitchNodeDescriptor();
        var parameters = ParseJson("""
            {
              "variable": "status",
              "cases": [{"value": "new"}]
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(1);
        await Assert.That(errors[0]).Contains("\"cases[0]\"");
        await Assert.That(errors[0]).Contains("\"branchKey\"");
    }

    [Test]
    public async Task Create_WithDuplicateValue_ThrowsInvalidOperation()
    {
        var descriptor = new SwitchNodeDescriptor();
        var parameters = ParseJson("""
            {
              "variable": "status",
              "cases": [
                {"value": "new", "branchKey": "branch_a"},
                {"value": "new", "branchKey": "branch_b"}
              ]
            }
            """);
        var services = Substitute.For<IServiceProvider>();

        var ex = Assert.Throws<InvalidOperationException>(
            () => descriptor.Create(Guid.NewGuid(), parameters, services));

        await Assert.That(ex.Message).Contains("\"new\"");
        await Assert.That(ex.Message).Contains("cases");
    }

    [Test]
    public async Task Create_AndExecute_BranchesByValue()
    {
        var descriptor = new SwitchNodeDescriptor();
        var parameters = ParseJson("""
            {
              "variable": "status",
              "cases": [
                {"value": "new", "branchKey": "case_new"},
                {"value": "done", "branchKey": "case_done"}
              ]
            }
            """);
        var services = Substitute.For<IServiceProvider>();

        var node = (SwitchNode)descriptor.Create(Guid.NewGuid(), parameters, services);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["status"] = "done" });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.BranchKey).IsEqualTo("case_done");
    }

    [Test]
    public async Task Create_AndExecute_NoMatch_ReturnsDefault()
    {
        var descriptor = new SwitchNodeDescriptor();
        var parameters = ParseJson("""
            {
              "variable": "status",
              "cases": [{"value": "new", "branchKey": "case_new"}]
            }
            """);
        var services = Substitute.For<IServiceProvider>();

        var node = (SwitchNode)descriptor.Create(Guid.NewGuid(), parameters, services);
        var context = CreateContext(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["status"] = "unknown" });

        var result = await node.ExecuteAsync(context);

        await Assert.That(result.BranchKey).IsEqualTo("default");
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;

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
