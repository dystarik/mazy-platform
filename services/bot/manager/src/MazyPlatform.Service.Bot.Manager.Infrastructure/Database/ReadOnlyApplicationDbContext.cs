namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Database;

using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Реализация <see cref="IReadOnlyApplicationDbContext"/> — оборачивает <see cref="ApplicationDbContext"/>
/// и возвращает запросы с отключённым отслеживанием изменений (<c>AsNoTracking</c>).
/// </summary>
/// <remarks>
/// Используется в обработчиках запросов (query side), которым не требуется сохранять изменения.
/// </remarks>
internal sealed class ReadOnlyApplicationDbContext(ApplicationDbContext context) : IReadOnlyApplicationDbContext
{
    /// <inheritdoc />
    public IQueryable<BotInstance> BotInstances => context.BotInstances.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<UserAccount> UserAccounts => context.UserAccounts.AsNoTracking();
}
