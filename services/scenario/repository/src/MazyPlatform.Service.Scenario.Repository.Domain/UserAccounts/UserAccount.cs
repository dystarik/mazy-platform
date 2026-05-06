namespace MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts;

using MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts.Events;

public sealed class UserAccount : AggregateRoot
{
    private UserAccount() { }

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
