namespace MazyPlatform.Service.Bot.Manager.Api.Configuration;

using Microsoft.AspNetCore.HttpOverrides;

using Serilog;

internal static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddConfigure(this WebApplicationBuilder builder)
    {
        builder.Services.AddAppServices(builder.Configuration);
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        builder.Host.UseSerilog((context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration));

        return builder;
    }
}
