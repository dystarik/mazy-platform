namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Repositories;

using MazyPlatform.Service.User.Authentication.Domain.UserSessions;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IUserSessionRepository"/>.
/// </summary>
internal sealed class UserSessionRepository(ApplicationDbContext context) : RepositoryBase<UserSession>(context), IUserSessionRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Если <paramref name="refreshTokenId"/> равен <see cref="Guid.Empty"/>.</exception>
    public async Task<UserSession?> GetByRefreshTokenIdAsync(Guid refreshTokenId, CancellationToken cancellationToken = default)
    {
        if (refreshTokenId == Guid.Empty)
            throw new ArgumentException("Идентификатор refresh-токена не может быть пустым.", nameof(refreshTokenId));

        return await _context.UserSessions.SingleOrDefaultAsync(s => s.RefreshTokenId == refreshTokenId, cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Если <paramref name="userAccountId"/> равен <see cref="Guid.Empty"/>.</exception>
    public async Task<IReadOnlyCollection<UserSession>> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("Идентификатор учетной записи пользователя не может быть пустым.", nameof(userAccountId));

        return await _context.UserSessions.Where(s => s.UserAccountId == userAccountId).ToListAsync(cancellationToken);
    }
}
