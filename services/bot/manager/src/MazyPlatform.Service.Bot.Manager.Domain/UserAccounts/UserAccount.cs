namespace MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;

using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts.Events;

/// <summary>
/// Агрегатный корень «Аккаунт пользователя» — локальная проекция аккаунта из <c>service-user-authentication</c>.
/// </summary>
public sealed class UserAccount : AggregateRoot
{
    private UserAccount() { }

    /// <summary>
    /// Создаёт локальную запись аккаунта пользователя и публикует <see cref="UserAccountCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта пользователя из <c>service-user-authentication</c>.</param>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>Созданный агрегат аккаунта пользователя.</returns>
    public static UserAccount Create(Guid userAccountId, DateTimeOffset now)
    {
        var account = new UserAccount
        {
            Id = userAccountId,
            CreatedAt = now,
        };

        account.AddDomainEvent(new UserAccountCreatedDomainEvent(now, userAccountId));

        return account;
    }
}
