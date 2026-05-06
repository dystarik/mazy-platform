namespace MazyPlatform.Service.Notification.Configuration.Options.RabbitMq;

using System.ComponentModel.DataAnnotations;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public required string Host { get; set; }

    [Range(1, 65535)]
    public required int Port { get; set; }

    [Required]
    public required string Username { get; set; }

    [Required]
    public required string Password { get; set; }

    [Required]
    public required string VirtualHost { get; set; }

    [Required]
    public required string ExchangeName { get; set; }

    [Required]
    public required string ExchangeType { get; set; }

    [Required]
    public required string DeadLetterExchangeName { get; set; }

    [Range(1, 65535)]
    public required ushort Prefetch { get; set; }

    [Range(1, 10)]
    public required int RetryCount { get; set; }

    [Range(1, 60)]
    public required int RetryDelaySeconds { get; set; }

    [MinLength(1)]
    public required List<RabbitMqQueueOptions> Queues { get; set; }
}
