namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database.Repositories;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IOneTimePasswordRepository"/>.
/// </summary>
internal sealed class OneTimePasswordRepository(ApplicationDbContext context) : RepositoryBase<OneTimePassword>(context), IOneTimePasswordRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Если <paramref name="userAccountId"/> равен <see cref="Guid.Empty"/>.</exception>
    public Task<OneTimePassword?> GetLatestUnverifiedByUserAccountIdAsync(Guid userAccountId, OtpType type, CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("Идентификатор учетной записи пользователя не может быть пустым.", nameof(userAccountId));

        return _context.OneTimePasswords
            .Where(otp => otp.UserAccountId == userAccountId
                && otp.Type == type
                && otp.VerifiedAt == null
                && otp.InvalidatedAt == null)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
