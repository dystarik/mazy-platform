namespace MazyPlatform.Scenario.Tests.Registry;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Telegram.Registration;
using MazyPlatform.Scenario.Vk.Registration;

using Microsoft.Extensions.DependencyInjection;

public class PlatformNodeCatalogTests
{
    [Test]
    public async Task GetAllMeta_Universal_ReturnsOnlyUniversalNodes()
    {
        using var serviceProvider = CreateServiceProvider();
        var catalog = serviceProvider.GetRequiredService<IPlatformNodeCatalog>();

        var types = catalog.GetAllMeta(ScenarioPlatformKeys.Universal)
            .Select(static meta => meta.Type)
            .ToArray();

        await Assert.That(types).Contains("send_message");
        await Assert.That(types).DoesNotContain("vk_send_keyboard");
    }

    [Test]
    public async Task GetAllMeta_Vk_ReturnsUniversalAndVkNodes()
    {
        using var serviceProvider = CreateServiceProvider();
        var catalog = serviceProvider.GetRequiredService<IPlatformNodeCatalog>();

        var types = catalog.GetAllMeta(ScenarioPlatformKeys.Vk)
            .Select(static meta => meta.Type)
            .ToArray();

        await Assert.That(types).Contains("send_message");
        await Assert.That(types).Contains("vk_send_keyboard");
    }

    [Test]
    public async Task GetAllMeta_Telegram_ReturnsUniversalWithoutVkNodes()
    {
        using var serviceProvider = CreateServiceProvider();
        var catalog = serviceProvider.GetRequiredService<IPlatformNodeCatalog>();

        var types = catalog.GetAllMeta(ScenarioPlatformKeys.Telegram)
            .Select(static meta => meta.Type)
            .ToArray();

        await Assert.That(types).Contains("send_message");
        await Assert.That(types).DoesNotContain("vk_send_keyboard");
    }

    private static ServiceProvider CreateServiceProvider() =>
        new ServiceCollection()
            .AddScenarioCatalog()
            .AddVkScenarioCatalog()
            .AddTelegramScenarioCatalog()
            .BuildServiceProvider();
}
