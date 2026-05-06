namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Repositories;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IMfaSessionRepository"/>.
/// </summary>
internal sealed class MfaSessionRepository(ApplicationDbContext context) : RepositoryBase<MfaSession>(context), IMfaSessionRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Если <paramref name="mfaSessionId"/> или <paramref name="userAccountId"/> равен <see cref="Guid.Empty"/>.
    /// </exception>
    public async Task<MfaSession?> GetByIdAndUserAccountIdAsync(Guid mfaSessionId, Guid userAccountId, CancellationToken cancellationToken = default)
    {
        if (mfaSessionId == Guid.Empty)
            throw new ArgumentException("Идентификатор сессии не может быть пустым.", nameof(mfaSessionId));

        if (userAccountId == Guid.Empty)
            throw new ArgumentException("Идентификатор учетной записи пользователя не может быть пустым.", nameof(userAccountId));

        return await _context.MfaSessions.SingleOrDefaultAsync(m => m.Id == mfaSessionId && m.UserAccountId == userAccountId, cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Если <paramref name="mfaSessionId"/> равен <see cref="Guid.Empty"/>.</exception>
    public async Task<MfaSession?> GetUnauthenticatedSessionByIdAsync(Guid mfaSessionId, CancellationToken cancellationToken = default)
    {
        if (mfaSessionId == Guid.Empty)
            throw new ArgumentException("Идентификатор сессии не может быть пустым.", nameof(mfaSessionId));

        return await _context.MfaSessions.SingleOrDefaultAsync(
            m => m.Id == mfaSessionId &&
                (m.Action == MfaSessionAction.Login || m.Action == MfaSessionAction.ResetPassword),
            cancellationToken);
    }
}
