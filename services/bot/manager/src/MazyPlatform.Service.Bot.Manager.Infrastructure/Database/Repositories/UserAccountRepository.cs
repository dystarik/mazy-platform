namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;

internal sealed class UserAccountRepository(ApplicationDbContext context)
    : RepositoryBase<UserAccount>(context), IUserAccountRepository
{
}
