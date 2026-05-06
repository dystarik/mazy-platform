namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Repositories;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IUserAccountRepository"/>.
/// </summary>
internal class UserAccountRepository(ApplicationDbContext context) : RepositoryBase<UserAccount>(context), IUserAccountRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> равен <see langword="null"/>.</exception>
    public async Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return await _context.UserAccounts.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> равен <see langword="null"/>.</exception>
    public async Task<UserAccount?> GetByUnverifiedEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return await _context.UserAccounts.SingleOrDefaultAsync(x => x.Email == email && x.EmailVerifiedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="externalProvider"/> равен <see langword="null"/>.</exception>
    public async Task<UserAccount?> GetByExternalProviderAsync(ExternalProvider externalProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(externalProvider);
        return await _context.UserAccounts.SingleOrDefaultAsync(x => x.UserLinkedProviders.LinkedProviders.Any(p => p.Type == externalProvider.Type && p.Email == externalProvider.Email), cancellationToken);
    }
}
