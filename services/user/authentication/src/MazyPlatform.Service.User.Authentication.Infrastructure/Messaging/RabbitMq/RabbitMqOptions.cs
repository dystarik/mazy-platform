namespace MazyPlatform.Service.User.Authentication.Infrastructure.Messaging.RabbitMq;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Параметры подключения к RabbitMQ, считываемые из секции <see cref="SectionName"/> файла настроек.
/// </summary>
internal sealed class RabbitMqOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "RabbitMq";

    /// <summary>Хост брокера RabbitMQ.</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string Host { get; init; }

    /// <summary>Порт брокера RabbitMQ. Допустимый диапазон: от 1 до 65535.</summary>
    [Range(1, 65535, ErrorMessage = "Значение {0} должно находиться в диапазоне от {1} до {2}.")]
    public required int Port { get; init; }

    /// <summary>Имя пользователя для аутентификации в RabbitMQ.</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string Username { get; init; }

    /// <summary>Пароль для аутентификации в RabbitMQ.</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string Password { get; init; }

    /// <summary>Виртуальный хост RabbitMQ.</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string VirtualHost { get; init; }

    /// <summary>Имя exchange, в который публикуются интеграционные события.</summary>
    [Required(ErrorMessage = "Поле {0} обязательно для заполнения.")]
    public required string ExchangeName { get; init; }
}
