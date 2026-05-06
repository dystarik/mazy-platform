namespace MazyPlatform.Scenario.Storage.Mongo.Registration;

using MazyPlatform.Scenario.Abstractions.Data;
using MazyPlatform.Scenario.Abstractions.Sessions;
using MazyPlatform.Scenario.Storage.Mongo.Configuration;
using MazyPlatform.Scenario.Storage.Mongo.Serialization;
using MazyPlatform.Scenario.Storage.Mongo.Stores;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

/// <summary>
/// Методы расширения для регистрации MongoDB-хранилищ сценариев.
/// </summary>
public static class MongoStorageServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует MongoDB-хранилища сессий, данных и схем.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Делегат настройки параметров MongoDB.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddMongoStorage(this IServiceCollection services, Action<MongoStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        MongoSerializationConfig.Configure();

        _ = services
            .AddOptions<MongoStorageOptions>()
            .Configure(configure);

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoStorageOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoStorageOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddScoped<ISessionStore, MongoSessionStore>();
        services.AddScoped<IDataStore, MongoDataStore>();
        services.AddScoped<ISchemaStore, MongoSchemaStore>();
        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }
}
