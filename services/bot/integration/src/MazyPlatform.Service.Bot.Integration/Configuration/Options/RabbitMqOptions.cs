namespace MazyPlatform.Service.Bot.Integration.Configuration.Options;

using System.ComponentModel.DataAnnotations;

using MazyPlatform.Service.Bot.Integration.Configuration.Options.RabbitMq;

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

    [Range(1, 1000)]
    public ushort Prefetch { get; set; } = 1;

    public List<RabbitMqQueueOptions> Queues { get; set; } = [];
}
