namespace MazyPlatform.Service.Scenario.Repository.Application;

using FluentValidation;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Scenario.Registration;
using MazyPlatform.Scenario.Telegram.Registration;
using MazyPlatform.Scenario.Vk.Registration;
using MazyPlatform.Service.Scenario.Repository.Application.Decorators;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Application.Abstractions.Events;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationLayer()
        {
            return services
                .AddScenarioCatalog()
                .AddVkScenarioCatalog()
                .AddTelegramScenarioCatalog()
                .AddValidators()
                .AddCommandHandlers()
                .AddQueryHandlers()
                .AddDecorators()
                .AddDomainEventHandlers()
                .AddIntegrationEventHandlers();
        }

        private IServiceCollection AddIntegrationEventHandlers()
        {
            return services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }

        private IServiceCollection AddValidators()
        {
            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        private IServiceCollection AddCommandHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        private IServiceCollection AddQueryHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }

        private IServiceCollection AddDecorators()
        {
            return services
                .Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandDecorator<,>))
                .Decorate(typeof(ICommandHandler<>), typeof(ValidationCommandDecoratorVoid<>))
                .Decorate(typeof(IQueryHandler<,>), typeof(ValidationQueryDecorator<,>));
        }

        private IServiceCollection AddDomainEventHandlers()
        {
            return services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }
    }
}
