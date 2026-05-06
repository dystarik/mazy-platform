namespace MazyPlatform.Service.Scenario.Repository.Infrastructure.Database;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs;
using MazyPlatform.Service.Scenario.Repository.Domain.Projects;
using MazyPlatform.Service.Scenario.Repository.Domain.Schemas;
using MazyPlatform.Service.Scenario.Repository.Domain.UserAccounts;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Основной контекст EF Core, предоставляющий доступ к записываемым наборам сущностей.
/// </summary>
internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    /// <summary>Набор записей <see cref="Project"/>.</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Набор записей <see cref="ScenarioGraph"/>.</summary>
    public DbSet<ScenarioGraph> ScenarioGraphs => Set<ScenarioGraph>();

    /// <summary>Набор записей <see cref="ScenarioVersion"/>.</summary>
    public DbSet<ScenarioVersion> ScenarioVersions => Set<ScenarioVersion>();

    /// <summary>Набор записей <see cref="EntitySchema"/>.</summary>
    public DbSet<EntitySchema> EntitySchemas => Set<EntitySchema>();

    /// <summary>Набор записей <see cref="EntityField"/>.</summary>
    public DbSet<EntityField> EntityFields => Set<EntityField>();

    /// <summary>Набор записей <see cref="UserAccount"/>.</summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
