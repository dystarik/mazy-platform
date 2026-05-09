namespace MazyPlatform.Scenario.Vk.UseCases;

using System.Text.Json;

internal static class VkButtonPayloadFormatter
{
    public static string Format(string payload) => JsonSerializer.Serialize(payload);
}
