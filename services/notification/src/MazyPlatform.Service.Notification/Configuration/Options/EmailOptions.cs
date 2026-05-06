namespace MazyPlatform.Service.Notification.Configuration.Options;

using System.ComponentModel.DataAnnotations;

internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public required string Host { get; set; }

    [Range(1, 65535)]
    public required int Port { get; set; }

    [Required]
    public required string Username { get; set; }

    [Required]
    public required string Password { get; set; }

    [Required]
    public required string From { get; set; }

    [Required]
    public required string DisplayName { get; set; }
}
