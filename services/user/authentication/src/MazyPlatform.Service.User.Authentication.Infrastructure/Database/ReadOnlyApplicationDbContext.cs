namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IReadOnlyApplicationDbContext"/> — оборачивает <see cref="ApplicationDbContext"/>
/// и возвращает запросы с отключённым отслеживанием изменений (<c>AsNoTracking</c>).
/// </summary>
/// <remarks>
/// Используется в обработчиках запросов (query side), которым не требуется сохранять изменения.
/// Отключение отслеживания снижает потребление памяти и повышает производительность чтения.
/// </remarks>
internal sealed class ReadOnlyApplicationDbContext(ApplicationDbContext context) : IReadOnlyApplicationDbContext
{
    /// <inheritdoc />
    public IQueryable<UserSession> UserSessions => context.UserSessions.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<UserAccount> UserAccounts => context.UserAccounts.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<MfaSettings> MfaSettings => context.MfaSettings.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<MfaMethod> MfaMethods => context.MfaMethods.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<OneTimePassword> OneTimePasswords => context.OneTimePasswords.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<MfaSession> MfaSessions => context.MfaSessions.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<UserLinkedProviders> LinkedProviders => context.LinkedProviders.AsNoTracking();
}
