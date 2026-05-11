namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class NodeCatalogTests : IntegrationTestBase
{
    [Test]
    public async Task GetNodeCatalog_Should_ReturnPlatformSpecificNodes()
    {
        var universal = await App.ScenarioGraphs.GetNodeCatalogAsync(new GetNodeCatalogRequest { PlatformType = PlatformType.Universal });
        var vk = await App.ScenarioGraphs.GetNodeCatalogAsync(new GetNodeCatalogRequest { PlatformType = PlatformType.Vk });
        var telegram = await App.ScenarioGraphs.GetNodeCatalogAsync(new GetNodeCatalogRequest { PlatformType = PlatformType.Telegram });

        await Assert.That(universal.Nodes.Any(n => string.Equals(n.Type, "send_message", StringComparison.Ordinal))).IsTrue();
        await Assert.That(vk.Nodes.Any(n => string.Equals(n.Type, "vk_send_keyboard", StringComparison.Ordinal))).IsTrue();
        await Assert.That(telegram.Nodes.Any(n => string.Equals(n.Type, "send_buttons", StringComparison.Ordinal))).IsTrue();
        await Assert.That(universal.Nodes.Any(n => n.Schema.Any(p => p.IsRequired))).IsTrue();
    }

    [Test]
    public async Task GetNodeCatalog_WithUnspecifiedPlatform_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetNodeCatalogAsync(
            new GetNodeCatalogRequest { PlatformType = PlatformType.Unspecified }));
    }
}
