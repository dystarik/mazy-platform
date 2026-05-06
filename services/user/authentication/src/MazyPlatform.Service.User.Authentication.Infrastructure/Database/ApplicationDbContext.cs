namespace MazyPlatform.Service.User.Authentication.Infrastructure.Database;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations.UserAccounts;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Основной контекст EF Core, предоставляющий доступ к записываемым наборам сущностей.
/// </summary>
/// <remarks>
/// Конфигурация <see cref="MfaMethod"/> передаётся через DI в виде
/// <see cref="IEntityTypeConfiguration{TEntity}"/>, поскольку <see cref="MfaMethodConfiguration"/>
/// требует <see cref="Infrastructure.Security.Totp.ITotpSecretEncryptor"/> для прозрачного шифрования
/// TOTP-секретов. Все остальные конфигурации применяются автоматически из текущей сборки,
/// за исключением <see cref="MfaMethodConfiguration"/>.
/// </remarks>
internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IEntityTypeConfiguration<MfaMethod> mfaMethodConfiguration) : DbContext(options)
{
    /// <summary>Набор записей <see cref="UserAccount"/>.</summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    /// <summary>Набор записей <see cref="MfaSettings"/>.</summary>
    public DbSet<MfaSettings> MfaSettings => Set<MfaSettings>();

    /// <summary>Набор записей <see cref="MfaMethod"/>.</summary>
    public DbSet<MfaMethod> MfaMethods => Set<MfaMethod>();

    /// <summary>Набор записей <see cref="UserSession"/>.</summary>
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    /// <summary>Набор записей <see cref="OneTimePassword"/>.</summary>
    public DbSet<OneTimePassword> OneTimePasswords => Set<OneTimePassword>();

    /// <summary>Набор записей <see cref="MfaSession"/>.</summary>
    public DbSet<MfaSession> MfaSessions => Set<MfaSession>();

    /// <summary>Набор записей <see cref="UserLinkedProviders"/>.</summary>
    public DbSet<UserLinkedProviders> LinkedProviders => Set<UserLinkedProviders>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(mfaMethodConfiguration);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly, t => t != typeof(MfaMethodConfiguration));
    }
}
