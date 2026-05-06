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

    [Test]
    public async Task Validate_SendButtons_ForUniversal_WithTooManyButtons_IsInvalid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateSendButtonsScenario(CreateButtonRows(11)), ScenarioPlatformKeys.Universal);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(static e => e.Code)).Contains("scenario.node_params_validation_failed");
        await Assert.That(result.Errors.Any(static e => e.Message.Contains("не больше 10"))).IsTrue();
    }

    [Test]
    public async Task Validate_SendButtons_ForVk_WithTooManyButtonsInRow_IsInvalid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateSendButtonsScenario(CreateButtonRows(6)), ScenarioPlatformKeys.Vk);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(static e => e.Message.Contains("buttons[0]"))).IsTrue();
        await Assert.That(result.Errors.Any(static e => e.Message.Contains("не больше 5"))).IsTrue();
    }

    [Test]
    public async Task Validate_VkKeyboard_ForVk_WithTooManyButtons_IsInvalid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateVkKeyboardScenario(CreateButtonRows(41, rowSize: 5)), ScenarioPlatformKeys.Vk);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Select(static e => e.Code)).Contains("scenario.node_params_validation_failed");
        await Assert.That(result.Errors.Any(static e => e.Message.Contains("не больше 40"))).IsTrue();
    }

    [Test]
    public async Task Validate_SendButtons_ForTelegram_WithMoreThanUniversalLimit_IsValid()
    {
        using var serviceProvider = CreateServiceProvider();
        var validator = serviceProvider.GetRequiredService<IPlatformScenarioValidator>();

        var result = validator.Validate(CreateSendButtonsScenario(CreateButtonRows(11)), ScenarioPlatformKeys.Telegram);

        await Assert.That(result.IsValid).IsTrue();
    }

    private static string CreateVkKeyboardScenario() =>
        CreateVkKeyboardScenario("""[[{ "label": "Да", "payload": "yes" }]]""");

    private static string CreateVkKeyboardScenario(string buttonsJson) =>
        $$"""
          {
            "startNodeId": "{{StartNodeId}}",
            "nodes": [
              {
                "id": "{{StartNodeId}}",
                "type": "vk_send_keyboard",
                "params": {
                  "text": "Выберите действие",
                  "buttons": {{buttonsJson}}
                }
              }
            ],
            "connections": []
          }
          """;

    private static string CreateSendButtonsScenario(string buttonsJson) =>
        $$"""
          {
            "startNodeId": "{{StartNodeId}}",
            "nodes": [
              {
                "id": "{{StartNodeId}}",
                "type": "send_buttons",
                "params": {
                  "text": "Выберите действие",
                  "buttons": {{buttonsJson}}
                }
              }
            ],
            "connections": []
          }
          """;

    private static string CreateButtonRows(int count, int rowSize = 100)
    {
        var buttons = Enumerable.Range(1, count)
            .Select(index => $$"""{ "label": "Кнопка {{index}}", "payload": "button_{{index}}" }""")
            .Chunk(rowSize)
            .Select(row => $"[{string.Join(", ", row)}]");

        return $"[{string.Join(", ", buttons)}]";
    }

    private static ServiceProvider CreateServiceProvider() =>
        new ServiceCollection()
            .AddScenarioCatalog()
            .AddVkScenarioCatalog()
            .AddTelegramScenarioCatalog()
            .BuildServiceProvider();
}
