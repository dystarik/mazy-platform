namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;
using MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts;

/// <summary>
/// Интерфейс контекста базы данных, предоставляющий доступ к наборам сущностей только для чтения.
/// </summary>
/// <remarks>
/// Используется в обработчиках запросов (query side), которым не требуется сохранять изменения.
/// Отключение отслеживания снижает потребление памяти и повышает производительность чтения.
/// </remarks>
public interface IReadOnlyApplicationDbContext
{
    /// <summary>Набор записей <see cref="Project"/>.</summary>
    IQueryable<Project> Projects { get; }

    /// <summary>Набор записей <see cref="ScenarioGraph"/>.</summary>
    IQueryable<ScenarioGraph> ScenarioGraphs { get; }

    /// <summary>Набор записей <see cref="EntitySchema"/>.</summary>
    IQueryable<EntitySchema> EntitySchemas { get; }

    /// <summary>Набор записей <see cref="UserAccount"/>.</summary>
    IQueryable<UserAccount> UserAccounts { get; }

    /// <summary>Набор записей <see cref="ScenarioVersion"/>.</summary>
    IQueryable<ScenarioVersion> ScenarioVersions { get; }
}
