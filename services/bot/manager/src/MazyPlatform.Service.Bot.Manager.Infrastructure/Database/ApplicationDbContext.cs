namespace MazyPlatform.Service.Bot.Manager.Infrastructure.Database;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances;
using MazyPlatform.Service.Bot.Manager.Domain.UserAccounts;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Database.Configurations;
using MazyPlatform.Service.Bot.Manager.Infrastructure.Security;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Основной контекст EF Core, предоставляющий доступ к записываемым наборам сущностей.
/// </summary>
internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IBotTokenEncryptor tokenEncryptor) : DbContext(options)
{
    /// <summary>Набор записей <see cref="BotInstance"/>.</summary>
    public DbSet<BotInstance> BotInstances => Set<BotInstance>();

    /// <summary>Набор записей <see cref="UserAccount"/>.</summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BotInstanceConfiguration(tokenEncryptor));
        modelBuilder.ApplyConfiguration(new UserAccountConfiguration());
    }
}
