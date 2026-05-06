namespace MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;

/// <summary>
/// Интерфейс контекста базы данных, предоставляющий доступ к наборам сущностей только для чтения.
/// </summary>
/// <remarks>
/// Используется в обработчиках запросов (query side), которым не требуется сохранять изменения.
/// </remarks>
public interface IReadOnlyApplicationDbContext
{
    /// <summary>Набор записей <see cref="UserSession"/>.</summary>
    IQueryable<UserSession> UserSessions { get; }

    /// <summary>Набор записей <see cref="UserAccount"/>.</summary>
    IQueryable<UserAccount> UserAccounts { get; }

    /// <summary>Набор записей <see cref="MfaSettings"/>.</summary>
    IQueryable<MfaSettings> MfaSettings { get; }

    /// <summary>Набор записей <see cref="MfaMethod"/>.</summary>
    IQueryable<MfaMethod> MfaMethods { get; }

    /// <summary>Набор записей <see cref="OneTimePassword"/>.</summary>
    IQueryable<OneTimePassword> OneTimePasswords { get; }

    /// <summary>Набор записей <see cref="MfaSession"/>.</summary>
    IQueryable<MfaSession> MfaSessions { get; }

    /// <summary>Набор записей <see cref="UserLinkedProviders"/>.</summary>
    IQueryable<UserLinkedProviders> LinkedProviders { get; }
}
