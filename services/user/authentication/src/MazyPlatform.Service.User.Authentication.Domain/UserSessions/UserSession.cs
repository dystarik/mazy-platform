namespace MazyPlatform.Service.User.Authentication.Domain.UserSessions;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;
using MazyPlatform.SharedKernel.Domain.Primitives;

/// <summary>
/// Агрегат пользовательской сессии, хранящей токен обновления.
/// </summary>
/// <remarks>
/// Создаётся при успешной аутентификации через <see cref="CreateNewSession"/>.
/// Поддерживает обновление токена (<see cref="RefreshSession"/>) и аннулирование (<see cref="Revoke"/>).
/// Максимальный срок жизни сессии — 7 дней от последнего обновления токена.
/// </remarks>
/// <seealso cref="RevokedReason"/>
/// <seealso cref="IUserSessionRepository"/>
public sealed class UserSession : AggregateRoot
{
    private const int _maxRefreshTokenAgeDays = 7;

    private UserSession() { }

    /// <summary>
    /// Идентификатор аккаунта, которому принадлежит данная сессия.
    /// </summary>
    public required Guid UserAccountId { get; init; }

    /// <summary>
    /// Идентификатор текущего токена обновления, используется для поиска сессии при refresh-запросах.
    /// </summary>
    public Guid RefreshTokenId { get; private set; }

    /// <summary>
    /// Хэш текущего токена обновления.
    /// </summary>
    public RefreshTokenHash RefreshTokenHash { get; private set; } = null!;

    /// <summary>
    /// Временная метка UTC истечения срока действия сессии.
    /// Обновляется при каждом успешном обновлении токена.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если <see cref="RevokedAt"/> не равно <see langword="null"/>.
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Временная метка UTC аннулирования сессии.
    /// Равна <see langword="null"/>, если сессия не аннулирована.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Причина аннулирования сессии. Равна <see langword="null"/>, если сессия не аннулирована.
    /// </summary>
    public RevokedReason? RevokedReason { get; private set; }

    /// <summary>
    /// Аннулирует сессию.
    /// </summary>
    /// <param name="reason">Причина аннулирования.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <remarks>
    /// Идемпотентный метод: повторный вызов для уже аннулированной сессии игнорируется.
    /// </remarks>
    public void Revoke(RevokedReason reason, DateTimeOffset now)
    {
        if (IsRevoked)
            return;

        RevokedAt = now;
        RevokedReason = reason;
        MarkAsUpdated(now);
    }

    /// <summary>
    /// Обновляет токен сессии, заменяя старый новым.
    /// </summary>
    /// <param name="hasher">Хэшер для верификации старого токена.</param>
    /// <param name="oldToken">Открытый старый токен, переданный клиентом.</param>
    /// <param name="newTokenHash">Хэш нового токена, вычисленный через <see cref="ITokenHasher"/>.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если токен обновлён;
    /// сбой с кодом <see cref="ErrorCodes.Auth.Unauthorized"/>, если сессия аннулирована, истекла
    /// или переданный токен не соответствует сохранённому хэшу.
    /// </returns>
    /// <remarks>
    /// Продлевает <see cref="ExpiresAt"/> на 7 дней от <paramref name="now"/>.
    /// Генерирует новый <see cref="RefreshTokenId"/> при каждом обновлении.
    /// </remarks>
    public Result RefreshSession(ITokenHasher hasher, string oldToken, RefreshTokenHash newTokenHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentException.ThrowIfNullOrWhiteSpace(oldToken);
        ArgumentNullException.ThrowIfNull(newTokenHash);

        if (IsRevoked)
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный или просроченный токен обновления.");

        if (ExpiresAt < now)
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный или просроченный токен обновления.");

        if (!hasher.Verify(oldToken, RefreshTokenHash.Value))
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный или просроченный токен обновления.");

        RefreshTokenId = Guid.NewGuid();
        RefreshTokenHash = newTokenHash;
        ExpiresAt = now.AddDays(_maxRefreshTokenAgeDays);
        MarkAsUpdated(now);

        return Result.Success();
    }

    /// <summary>
    /// Создаёт новую пользовательскую сессию.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта пользователя.</param>
    /// <param name="refreshTokenHash">Хэш первого токена обновления.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// Новая <see cref="UserSession"/> со сроком действия <c>_maxRefreshTokenAgeDays</c> дней.
    /// </returns>
    /// <exception cref="ArgumentException">Если <paramref name="userAccountId"/> является пустым Guid.</exception>
    /// <exception cref="ArgumentNullException">Если <paramref name="refreshTokenHash"/> равен <see langword="null"/>.</exception>
    internal static UserSession CreateNewSession(Guid userAccountId, RefreshTokenHash refreshTokenHash, DateTimeOffset now)
    {
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("userAccountId не может быть пустым.", nameof(userAccountId));
        ArgumentNullException.ThrowIfNull(refreshTokenHash);

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            RefreshTokenId = Guid.NewGuid(),
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = now.AddDays(_maxRefreshTokenAgeDays),
            CreatedAt = now,
        };
    }
}
