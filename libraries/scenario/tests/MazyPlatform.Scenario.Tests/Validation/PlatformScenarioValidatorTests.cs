namespace MazyPlatform.Scenario.Tests.Validation;

using MazyPlatform.Scenario.Abstractions.Platforms;
using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Telegram.Registration;
using MazyPlatform.Scenario.Vk.Registration;

using Microsoft.Extensions.DependencyInjection;

public class PlatformScenarioValidatorTests
{
    private static readonly Guid StartNodeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public async Task Validate_VkNode_ForVk_IsValid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateVkKeyboardScenario(), ScenarioPlatformKeys.Vk);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_VkNode_ForTelegram_IsInvalid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateVkKeyboardScenario(), ScenarioPlatformKeys.Telegram);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(static e => e.Code)).Contains("scenario.unknown_node_type");
    }

    [Test]
    public async Task Validate_VkNode_ForUniversal_IsInvalid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateVkKeyboardScenario(), ScenarioPlatformKeys.Universal);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(static e => e.Code)).Contains("scenario.unknown_node_type");
    }

    private static string CreateVkKeyboardScenario() =>
        $$"""
          {
            "startNodeId": "{{StartNodeId}}",
            "nodes": [
              {
                "id": "{{StartNodeId}}",
                "type": "vk_send_keyboard",
                "params": {
                  "text": "Выберите действие",
                  "buttons": [[{ "label": "Да", "payload": "yes" }]]
                }
              }
            ],
            "connections": []
          }
          """;

    private static ServiceProvider CreateServiceProvider() =>
        new ServiceCollection()
            .AddScenarioCatalog()
            .AddVkScenarioCatalog()
            .AddTelegramScenarioCatalog()
            .BuildServiceProvider();
}
