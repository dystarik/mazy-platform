namespace MazyPlatform.Contracts.User.Authentication.Events;

/// <summary>
/// Базовый класс для интеграционных событий аутентификации связанных с электронной почтой аккаунта пользователя.
/// </summary>
public abstract record UserAccountEmailIntegrationEventBase : IntegrationEventBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserAccountEmailIntegrationEventBase"/>.
    /// </summary>
    /// <param name="occurredAt">Временная метка возникновения события.</param>
    /// <param name="userAccountId">Уникальный идентификатор аккаунта пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    protected UserAccountEmailIntegrationEventBase(
        DateTimeOffset occurredAt,
        Guid userAccountId,
        string email)
        : base(occurredAt)
    {
        UserAccountId = userAccountId;
        Email = email;
    }

    /// <summary>
    /// Уникальный идентификатор аккаунта пользователя.
    /// </summary>
    public Guid UserAccountId { get; }

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    public string Email { get; }
}
