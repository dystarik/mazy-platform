namespace MazyPlatform.Service.User.Authentication.Api.Configuration;

using MazyPlatform.Service.User.Authentication.Api.Interceptors;
using MazyPlatform.Service.User.Authentication.Application;
using MazyPlatform.Service.User.Authentication.Domain;
using MazyPlatform.Service.User.Authentication.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAppServices(IConfiguration configuration)
        {
            return services
                .AddApiLayer(configuration)
                .AddApplicationLayer()
                .AddDomainLayer()
                .AddInfrastructureLayer(configuration);
        }

        private IServiceCollection AddApiLayer(IConfiguration configuration)
        {
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<TraceIdInterceptor>();
                options.Interceptors.Add<LoggingInterceptor>();
            });

            return services
                .AddSingleton(TimeProvider.System);
        }
    }
}
