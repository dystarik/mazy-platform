namespace MazyPlatform.Service.User.Authentication.Api.Configuration;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Serilog;

internal static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddConfigure(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Services.AddAppServices(configuration);
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<AuthenticationConfigurationHealthCheck>("configuration", tags: ["ready"]);

        builder.Host.UseSerilog((context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration));

        return builder;
    }
}
