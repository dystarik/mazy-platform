namespace MazyPlatform.SharedKernel.Infrastructure;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Events;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Infrastructure.Dispatchers;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Регистрация инфраструктурных компонентов Shared Kernel в Dependency Injection контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует инфраструктурные сервисы Shared Kernel.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    /// <returns>Тот же экземпляр <see cref="IServiceCollection"/> для удобной цепочки вызовов.</returns>
    /// <remarks>
    /// Сейчас регистрируется:
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="ICommandDispatcher"/> с реализацией <see cref="CommandDispatcher"/> как <c>Scoped</c>.</description>
    /// </item>
    /// <item>
    /// <description><see cref="IQueryDispatcher"/> с реализацией <see cref="QueryDispatcher"/> как <c>Scoped</c>.</description>
    /// </item>
    /// <item>
    /// <description><see cref="IDomainEventDispatcher"/> с реализацией <see cref="DomainEventDispatcher"/> как <c>Scoped</c>.</description>
    /// </item>
    /// </list>
    /// В ASP.NET Core <c>Scoped</c> означает один экземпляр на HTTP-запрос.
    /// </remarks>
    public static IServiceCollection AddSharedKernelInfrastructure(this IServiceCollection services)
    {
        return services
            .AddScoped<ICommandDispatcher, CommandDispatcher>()
            .AddScoped<IQueryDispatcher, QueryDispatcher>()
            .AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    }
}
