namespace MazyPlatform.Service.User.Authentication.Infrastructure;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Infrastructure.Database;
using MazyPlatform.Service.User.Authentication.Infrastructure.Database.Configurations.UserAccounts;
using MazyPlatform.Service.User.Authentication.Infrastructure.ExternalProviders;
using MazyPlatform.Service.User.Authentication.Infrastructure.ExternalProviders.Yandex;
using MazyPlatform.Service.User.Authentication.Infrastructure.Messaging.RabbitMq;
using MazyPlatform.Service.User.Authentication.Infrastructure.Security;
using MazyPlatform.Service.User.Authentication.Infrastructure.Security.Jwt;
using MazyPlatform.Service.User.Authentication.Infrastructure.Security.Totp;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructureLayer(IConfiguration configuration)
        {
            return services
                .AddSharedKernelInfrastructure()
                .AddDatabase(configuration)
                .AddExternalProviders()
                .AddMessaging()
                .AddSecurity();
        }

        private IServiceCollection AddDatabase(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Строка подключения «DefaultConnection» не найдена.");

            services.AddDbContext<ApplicationDbContext>(contextOptions => contextOptions.UseNpgsql(connectionString));

            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(c => c.AssignableTo(typeof(IRepository<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services
                .AddScoped<IUnitOfWork, UnitOfWork>()
                .AddScoped<IReadOnlyApplicationDbContext, ReadOnlyApplicationDbContext>();
        }

        private IServiceCollection AddExternalProviders()
        {
            services.AddOptions<YandexProviderOptions>()
                .BindConfiguration(YandexProviderOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpClient("Yandex");

            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(c => c.AssignableTo<IProviderHandler>(), publicOnly: false)
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            return services
                .AddSingleton<IExternalProviderService, ExternalProviderService>();
        }

        private IServiceCollection AddMessaging()
        {
            services.AddOptions<RabbitMqOptions>()
                .BindConfiguration(RabbitMqOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<RabbitMqConnectionFactory>();
            services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
            services.AddHostedService<RabbitMqStartupService>();

            return services;
        }

        private IServiceCollection AddSecurity()
        {
            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<TotpEncryptionOptions>()
                .BindConfiguration(TotpEncryptionOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<TotpOptions>()
                .BindConfiguration(TotpOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<ITotpService, TotpService>();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddSingleton<IEntityTypeConfiguration<MfaMethod>, MfaMethodConfiguration>();

            services
                .AddSingleton<Argon2idService>()
                .AddSingleton<IPasswordHasher>(sp => sp.GetRequiredService<Argon2idService>())
                .AddSingleton<ITokenHasher>(sp => sp.GetRequiredService<Argon2idService>());

            services
                .AddSingleton<ITotpSecretEncryptor, AesGcmService>();

            services
                .AddScoped<HmacSha256Service>()
                .AddScoped<IBackupCodeHasher>(sp => sp.GetRequiredService<HmacSha256Service>())
                .AddScoped<ICodeHasher>(sp => sp.GetRequiredService<HmacSha256Service>());

            return services;
        }
    }
}
