namespace MazyPlatform.Contracts.Core;

/// <summary>
/// Атрибут для указания ключа маршрутизации интеграционного события.
/// Используется диспетчером для построения мапы routing key → обработчик при старте приложения.
/// </summary>
/// <remarks>
/// Каждый класс реализующий <see cref="IIntegrationEvent"/> обязан иметь этот атрибут.
/// Отсутствие атрибута приведёт к исключению при старте приложения.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventTypeAttribute(string routingKey) : Attribute
{
    /// <summary>
    /// Ключ маршрутизации события в брокере сообщений.
    /// </summary>
    /// <value>Строка в формате <c>{область}.{сервис}.{действие}</c>, например <c>user.authentication.registered</c>.</value>
    public string RoutingKey { get; } = routingKey;
}
