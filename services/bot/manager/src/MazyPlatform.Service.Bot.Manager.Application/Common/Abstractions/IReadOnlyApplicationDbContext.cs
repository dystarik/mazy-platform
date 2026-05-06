namespace MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;

/// <summary>
/// Интерфейс контекста базы данных, предоставляющий доступ к наборам сущностей только для чтения.
/// </summary>
/// <remarks>
/// Используется в обработчиках запросов (query side), которым не требуется сохранять изменения.
/// Отключение отслеживания снижает потребление памяти и повышает производительность чтения.
/// </remarks>
public interface IReadOnlyApplicationDbContext
{
    /// <summary>Набор записей <see cref="BotInstance"/>.</summary>
    IQueryable<BotInstance> BotInstances { get; }

    /// <summary>Набор записей <see cref="UserAccount"/>.</summary>
    IQueryable<UserAccount> UserAccounts { get; }
}
