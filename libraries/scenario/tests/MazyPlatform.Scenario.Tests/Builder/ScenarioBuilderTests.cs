namespace MazyPlatform.Scenario.Tests.Builder;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Builder;
using MazyPlatform.Scenario.Nodes.Messages;
using MazyPlatform.Scenario.Tests.Helpers;

using NSubstitute;

public class ScenarioBuilderTests
{
    private const string FullJson = """
        {
          "startNodeId": "11111111-1111-1111-1111-111111111111",
          "nodes": [
            { "id": "11111111-1111-1111-1111-111111111111", "type": "test", "params": {} },
            { "id": "22222222-2222-2222-2222-222222222222", "type": "test", "params": {} },
            { "id": "33333333-3333-3333-3333-333333333333", "type": "test" }
          ],
          "connections": [
            { "from": "11111111-1111-1111-1111-111111111111", "to": "22222222-2222-2222-2222-222222222222", "branch": "default" },
            { "from": "22222222-2222-2222-2222-222222222222", "to": "33333333-3333-3333-3333-333333333333", "branch": "default" }
          ]
        }
        """;

    private static readonly Guid Node1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Node2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Node3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Test]
    public async Task Build_ParsesStartNodeId()
    {
        var builder = CreateBuilder();

        var graph = builder.Build(FullJson);

        await Assert.That(graph.StartNodeId).IsEqualTo(Node1Id);
    }

    [Test]
    public async Task Build_CreatesAllNodesFromJson()
    {
        var builder = CreateBuilder();

        var graph = builder.Build(FullJson);

        await Assert.That(graph.Nodes.Count).IsEqualTo(3);
        await Assert.That(graph.Nodes.ContainsKey(Node1Id)).IsTrue();
        await Assert.That(graph.Nodes.ContainsKey(Node2Id)).IsTrue();
        await Assert.That(graph.Nodes.ContainsKey(Node3Id)).IsTrue();
    }

    [Test]
    public async Task Build_CreatesConnections()
    {
        var builder = CreateBuilder();

        var graph = builder.Build(FullJson);

        await Assert.That(graph.Connections.Count).IsEqualTo(2);
        await Assert.That(graph.Connections.ContainsKey(Node1Id)).IsTrue();
        await Assert.That(graph.Connections[Node1Id][0].ToNodeId).IsEqualTo(Node2Id);
        await Assert.That(graph.Connections[Node2Id][0].ToNodeId).IsEqualTo(Node3Id);
    }

    [Test]
    public async Task Build_EmptyJson_ThrowsException()
    {
        var builder = CreateBuilder();

        await Assert.That(() => builder.Build(string.Empty)).ThrowsException();
    }

    [Test]
    public async Task Build_WithoutStartNodeId_ThrowsException()
    {
        var builder = CreateBuilder();
        var json = """{ "nodes": [], "connections": [] }""";

        await Assert.That(() => builder.Build(json)).ThrowsException();
    }

    [Test]
    public async Task Build_WithoutNodes_ThrowsException()
    {
        var builder = CreateBuilder();
        var json = """{ "startNodeId": "11111111-1111-1111-1111-111111111111", "connections": [] }""";

        await Assert.That(() => builder.Build(json)).ThrowsException();
    }

    [Test]
    public async Task Build_NodeWithoutParams_Works()
    {
        var builder = CreateBuilder();
        var json = """
            {
              "startNodeId": "11111111-1111-1111-1111-111111111111",
              "nodes": [
                { "id": "11111111-1111-1111-1111-111111111111", "type": "test" }
              ],
              "connections": []
            }
            """;

        var graph = builder.Build(json);

        await Assert.That(graph.Nodes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Build_CollectsEntryPoints_FromReceiveButtonPressNodes()
    {
        var builder = CreateBuilderWithEntryPoints();
        var json = """
            {
              "startNodeId": "11111111-1111-1111-1111-111111111111",
              "nodes": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "btn", "expectedPayloads": ["menu", "start"], "isEntry": true }
                },
                {
                  "id": "22222222-2222-2222-2222-222222222222",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "btn", "expectedPayloads": ["help"], "isEntry": true }
                }
              ],
              "connections": []
            }
            """;

        var graph = builder.Build(json);

        await Assert.That(graph.EntryPoints.Count).IsEqualTo(3);
        await Assert.That(graph.EntryPoints["menu"]).IsEqualTo(Node1Id);
        await Assert.That(graph.EntryPoints["start"]).IsEqualTo(Node1Id);
        await Assert.That(graph.EntryPoints["help"]).IsEqualTo(Node2Id);
    }

    [Test]
    public async Task Build_NoEntryPoints_ReturnsEmptyDictionary()
    {
        var builder = CreateBuilder();

        var graph = builder.Build(FullJson);

        await Assert.That(graph.EntryPoints.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Build_DuplicatePayloadInEntryPoints_ThrowsException()
    {
        var builder = CreateBuilderWithEntryPoints();
        var json = """
            {
              "startNodeId": "11111111-1111-1111-1111-111111111111",
              "nodes": [
                {
                  "id": "11111111-1111-1111-1111-111111111111",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "btn", "expectedPayloads": ["menu"], "isEntry": true }
                },
                {
                  "id": "22222222-2222-2222-2222-222222222222",
                  "type": "receive_button_press",
                  "params": { "buttonPayloadVariable": "btn", "expectedPayloads": ["menu"], "isEntry": true }
                }
              ],
              "connections": []
            }
            """;

        await Assert.That(() => builder.Build(json)).ThrowsException();
    }

    private static ScenarioBuilder CreateBuilder()
    {
        var registry = Substitute.For<INodeRegistry>();
        registry.Resolve(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<JsonElement>())
            .Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(1);
                var type = callInfo.ArgAt<string>(0);
                return new TestNode(id, type, false, _ => Task.FromResult(NodeResult.Continue()));
            });
        return new ScenarioBuilder(registry);
    }

    private static ScenarioBuilder CreateBuilderWithEntryPoints()
    {
        var registry = Substitute.For<INodeRegistry>();
        registry.Resolve(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<JsonElement>())
            .Returns(callInfo =>
            {
                var type = callInfo.ArgAt<string>(0);
                var id = callInfo.ArgAt<Guid>(1);
                var parameters = callInfo.ArgAt<JsonElement>(2);

                if (string.Equals(type, "receive_button_press", StringComparison.Ordinal))
                {
                    var payloads = parameters.TryGetProperty("expectedPayloads", out var payloadsElement)
                        ? payloadsElement.EnumerateArray().Select(e => e.GetString()!).ToList()
                        : null;
                    var isEntry = parameters.TryGetProperty("isEntry", out var isEntryElement)
                        && isEntryElement.GetBoolean();

                    return new ReceiveButtonPressNode(
                        id,
                        "btn",
                        Substitute.For<IReceiveButtonPressUseCase>(),
                        payloads,
                        isEntry);
                }

                return new TestNode(id, type, false, _ => Task.FromResult(NodeResult.Continue()));
            });
        return new ScenarioBuilder(registry);
    }
}
