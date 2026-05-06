namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database.Repositories;

using MazyPlatform.Service.Scenario.Repository.Infrastructure.Database;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Primitives;

/// <summary>
/// Базовая реализация <see cref="IRepository{TEntity}"/> для агрегатных корней.
/// Предоставляет операции добавления, обновления, удаления и поиска по идентификатору
/// через <see cref="ApplicationDbContext"/>.
/// </summary>
/// <typeparam name="TEntity">Тип агрегатного корня, наследующего <see cref="AggregateRoot"/>.</typeparam>
internal abstract class RepositoryBase<TEntity>(ApplicationDbContext context) : IRepository<TEntity>
    where TEntity : AggregateRoot
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="entity"/> равен <see langword="null"/>.</exception>
    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Add(entity);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="entity"/> равен <see langword="null"/>.</exception>
    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Update(entity);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Если <paramref name="entity"/> равен <see langword="null"/>.</exception>
    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Remove(entity);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Если <paramref name="id"/> равен <see cref="Guid.Empty"/>.</exception>
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификатор сущности не может быть пустым.", nameof(id));

        return await _context.Set<TEntity>().FindAsync([id], cancellationToken);
    }
}
