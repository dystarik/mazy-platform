namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Database;

using MazyPlatform.SharedKernel.Application.Abstractions.Events;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Primitives;

/// <summary>
/// Реализация <see cref="IUnitOfWork"/> — сохраняет изменения EF Core и диспетчеризует доменные события
/// до тех пор, пока обработчики не перестают порождать новые события.
/// </summary>
/// <remarks>
/// Перед каждым вызовом <see cref="ApplicationDbContext.SaveChangesAsync"/> извлекаются и очищаются
/// доменные события всех отслеживаемых агрегатов (<see cref="AggregateRoot"/>).
/// Цикл продолжается, пока обработчики событий не вызовут изменений, сами порождающих новые события.
/// </remarks>
internal class UnitOfWork(ApplicationDbContext context, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IDomainEvent[] domainEvents;
        do
        {
            // TODO: Добавить сохранение доменных событий в базу данных, чтобы не потерять их при сбое
            domainEvents = [.. context.ChangeTracker.Entries<AggregateRoot>().SelectMany(x => x.Entity.GetAndClearDomainEvents())];

            await context.SaveChangesAsync(cancellationToken);

            if (domainEvents.Length > 0)
                await dispatcher.DispatchAsync(domainEvents, cancellationToken);
        } while (domainEvents.Length is not 0);
    }
}
