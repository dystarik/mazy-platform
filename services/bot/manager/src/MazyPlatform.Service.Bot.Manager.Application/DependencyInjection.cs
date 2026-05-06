namespace MazyPlatform.Service.Bot.Manager.Application;

using FluentValidation;

using MazyPlatform.Contracts.Core;
using MazyPlatform.Service.Bot.Manager.Application.Common.Abstractions.Streaming;
using MazyPlatform.Service.Bot.Manager.Application.Decorators;
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
                .AddValidators()
                .AddCommandHandlers()
                .AddQueryHandlers()
                .AddStreamingQueryHandlers()
                .AddDecorators()
                .AddDomainEventHandlers()
                .AddIntegrationEventHandlers();
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
            services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationCommandDecorator<,>));
            services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationCommandDecoratorVoid<>));
            services.TryDecorate(typeof(IQueryHandler<,>), typeof(ValidationQueryDecorator<,>));
            return services;
        }

        private IServiceCollection AddStreamingQueryHandlers()
        {
            services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IStreamingQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddScoped<IStreamingQueryDispatcher, StreamingQueryDispatcher>();

            return services;
        }

        private IServiceCollection AddDomainEventHandlers()
        {
            return services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }

        private IServiceCollection AddIntegrationEventHandlers()
        {
            return services.Scan(scan => scan
                .FromAssemblies(typeof(DependencyInjection).Assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }
    }
}
