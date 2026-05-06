namespace MazyPlatform.Scenario.Tests.Registry;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Registry;

public class NodeCatalogTests
{
    [Test]
    public async Task Register_DuplicateRegistration_ThrowsInvalidOperationException()
    {
        var catalog = new NodeCatalog();

        catalog.Register(
            "send_message",
            [
                new("text", NodeParamType.String, IsRequired: true),
            ]);

        await Assert.That(() => catalog.Register(
                "send_message",
                [
                    new("text", NodeParamType.String, IsRequired: true),
                ]))
            .ThrowsExactly<InvalidOperationException>();
    }
}
