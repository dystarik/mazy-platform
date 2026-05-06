namespace MazyPlatform.Scenario.Tests.Nodes.Data;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Data;

public class QueryRecordsNodeDescriptorTests
{
    [Test]
    public async Task Schema_HasOptionalCountVariable()
    {
        var descriptor = new QueryRecordsNodeDescriptor();

        var param = descriptor.Schema.Single(
            p => string.Equals(p.Key, "countVariable", StringComparison.Ordinal));

        await Assert.That(param.Type).IsEqualTo(NodeParamType.String);
        await Assert.That(param.IsRequired).IsFalse();
        await Assert.That(param.Description).IsNotNull();
        await Assert.That(param.Description).IsNotEmpty();
    }

    [Test]
    public async Task Validate_WithCountVariable_ReturnsNoErrors()
    {
        var descriptor = new QueryRecordsNodeDescriptor();
        var parameters = ParseJson("""
            {
              "entityName": "Заявка",
              "recordsVariable": "records",
              "countVariable": "appointmentsCount"
            }
            """);

        var errors = descriptor.Validate(Guid.NewGuid(), parameters);

        await Assert.That(errors.Count).IsEqualTo(0);
    }

    private static JsonElement ParseJson(string json) =>
        JsonDocument.Parse(json).RootElement;
}
