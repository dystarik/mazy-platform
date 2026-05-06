namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database;

using MazyPlatform.SharedKernel.Application.Abstractions.Events;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Primitives;

/// <summary>
/// Реализация <see cref="IUnitOfWork"/> — сохраняет изменения EF Core и диспетчеризует доменные события
/// до тех пор, пока обработчики не перестают порождать новые события.
/// </summary>
/// <remarks>
/// После каждого вызова <see cref="ApplicationDbContext.SaveChangesAsync"/> извлекаются и очищаются
/// доменные события всех отслеживаемых агрегатов (<see cref="AggregateRoot"/>).
/// Цикл продолжается, пока обработчики событий не вызовут изменений, сами порождающих новые события.
/// </remarks>
internal class UnitOfWork(ApplicationDbContext context, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);

        IDomainEvent[] domainEvents;
        do
        {
            // TODO: Добавить сохранение доменных событий в базу данных, чтобы не потерять их при сбое
            domainEvents = context.ChangeTracker
                .Entries<AggregateRoot>()
                .SelectMany(x => x.Entity.GetAndClearDomainEvents())
                .ToArray();

            if (domainEvents.Length is 0) continue;

            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        } while (domainEvents.Length is not 0);
    }
}
