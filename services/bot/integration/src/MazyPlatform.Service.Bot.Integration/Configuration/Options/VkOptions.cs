namespace MazyPlatform.Service.Bot.Integration.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class VkOptions
{
    public const string SectionName = "Vk";

    /// <summary>Версия VK API.</summary>
    [Required]
    public required string ApiVersion { get; set; }

    /// <summary>Время ожидания Long Poll запроса в секундах.</summary>
    [Range(1, 90)]
    public int WaitSeconds { get; set; } = 25;
}
