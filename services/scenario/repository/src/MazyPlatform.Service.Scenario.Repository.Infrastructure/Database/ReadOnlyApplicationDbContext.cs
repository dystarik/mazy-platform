namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;
using MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts;

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
    public IQueryable<Project> Projects => context.Projects.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<ScenarioGraph> ScenarioGraphs => context.ScenarioGraphs.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<EntitySchema> EntitySchemas => context.EntitySchemas.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<UserAccount> UserAccounts => context.UserAccounts.AsNoTracking();

    /// <inheritdoc />
    public IQueryable<ScenarioVersion> ScenarioVersions => context.ScenarioVersions.AsNoTracking();
}
