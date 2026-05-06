namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts;

/// <summary>
/// Реализация <see cref="IUserAccountRepository"/>.
/// </summary>
internal sealed class UserAccountRepository(ApplicationDbContext context) : RepositoryBase<UserAccount>(context), IUserAccountRepository;
