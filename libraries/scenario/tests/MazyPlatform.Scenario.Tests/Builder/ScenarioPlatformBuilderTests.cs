namespace MazyPlatform.Scenario.Tests.Builder;

using System.Reflection;

using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Abstractions.Scenarios;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Telegram.Registration;
using MazyPlatform.Scenario.Telegram.UseCases;
using MazyPlatform.Scenario.Vk.Registration;
using MazyPlatform.Scenario.Vk.UseCases;

using Microsoft.Extensions.DependencyInjection;

public class ScenarioPlatformBuilderTests
{
    private static readonly Guid StartNodeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public async Task Build_SameJson_UsesPlatformSpecificUseCases()
    {
        using var serviceProvider = new ServiceCollection()
            .AddScenario()
            .AddVkScenario(_ => { })
            .AddTelegramScenario(_ => { })
            .BuildServiceProvider();

        var resolver = serviceProvider.GetRequiredService<IScenarioPlatformBuilderResolver>();
        var vkGraph = resolver.Resolve(ScenarioPlatformKeys.Vk).Build(CreateSendMessageScenario());
        var telegramGraph = resolver.Resolve(ScenarioPlatformKeys.Telegram).Build(CreateSendMessageScenario());

        var vkUseCase = GetUseCase<ISendMessageUseCase>(vkGraph.Nodes[StartNodeId]);
        var telegramUseCase = GetUseCase<ISendMessageUseCase>(telegramGraph.Nodes[StartNodeId]);

        await Assert.That(vkUseCase).IsTypeOf<VkSendMessageUseCase>();
        await Assert.That(telegramUseCase).IsTypeOf<TelegramSendMessageUseCase>();
    }

    private static TUseCase GetUseCase<TUseCase>(object node)
    {
        return node.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.GetValue(node))
            .OfType<TUseCase>()
            .Single();
    }

    private static string CreateSendMessageScenario() =>
        $$"""
          {
            "startNodeId": "{{StartNodeId}}",
            "nodes": [
              {
                "id": "{{StartNodeId}}",
                "type": "send_message",
                "params": { "text": "Привет" }
              }
            ],
            "connections": []
          }
          """;
}
