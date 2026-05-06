namespace MazyPlatform.Scenario.Tests.Registry;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Registry;
using MazyPlatform.Scenario.Tests.Helpers;

public class NodeRegistryTests
{
    [Test]
    public async Task Register_And_Resolve_CreatesNodeByType()
    {
        var registry = new NodeRegistry();
        var expectedId = Guid.NewGuid();

        registry.Register("test_node", (id, _) =>
            new TestNode(id, "test_node", false, _ => Task.FromResult(NodeResult.Continue())));

        var node = registry.Resolve("test_node", expectedId, default);

        await Assert.That(node.NodeId).IsEqualTo(expectedId);
        await Assert.That(node.NodeType).IsEqualTo("test_node");
    }

    [Test]
    public async Task Resolve_UnregisteredType_ThrowsInvalidOperationException()
    {
        var registry = new NodeRegistry();

        await Assert.That(() => registry.Resolve("unknown", Guid.NewGuid(), default))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task IsRegistered_ReturnsTrue_ForRegisteredType()
    {
        var registry = new NodeRegistry();
        registry.Register("test_node", (id, _) =>
            new TestNode(id, "test_node", false, _ => Task.FromResult(NodeResult.Continue())));

        await Assert.That(registry.IsRegistered("test_node")).IsTrue();
    }

    [Test]
    public async Task IsRegistered_ReturnsFalse_ForUnregisteredType()
    {
        var registry = new NodeRegistry();

        await Assert.That(registry.IsRegistered("unknown")).IsFalse();
    }

    [Test]
    public async Task Register_DuplicateRegistration_ThrowsInvalidOperationException()
    {
        var registry = new NodeRegistry();

        registry.Register("test_node", (id, _) =>
            new TestNode(id, "first", false, _ => Task.FromResult(NodeResult.Continue())));

        await Assert.That(() => registry.Register("test_node", (id, _) =>
                new TestNode(id, "second", false, _ => Task.FromResult(NodeResult.Continue()))))
            .ThrowsExactly<InvalidOperationException>();
    }
}
