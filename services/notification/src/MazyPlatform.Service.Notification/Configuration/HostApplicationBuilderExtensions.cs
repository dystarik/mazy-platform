namespace MazyPlatform.Service.Notification.Configuration;

using MazyPlatform.Service.Notification.Observability;

internal static class HostApplicationBuilderExtensions
{
    public static HostApplicationBuilder AddConfigure(this HostApplicationBuilder builder)
    {
        builder.Services
            .AddHostedService<DiagnosticsHostedService>()
            .AddAppServices(builder.Configuration);

        return builder;
    }
}
