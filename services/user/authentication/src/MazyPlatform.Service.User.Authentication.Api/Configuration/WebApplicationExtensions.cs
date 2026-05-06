namespace MazyPlatform.Service.User.Authentication.Api.Configuration;

using MazyPlatform.Service.User.Authentication.Api.Services;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Prometheus;

internal static class WebApplicationExtensions
{
    public static WebApplication UseMiddleware(this WebApplication app)
    {
        var port = app.Configuration.GetValue<int>("MetricsPort");
        var metricServer = new KestrelMetricServer(port);
        metricServer.Start();

        app.UseForwardedHeaders();
        app.UseHttpMetrics();
        app.UseGrpcMetrics();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        });

        app.MapGrpcService<RegistrationGrpcService>();
        app.MapGrpcService<AuthenticationGrpcService>();
        app.MapGrpcService<PasswordGrpcService>();
        app.MapGrpcService<MfaGrpcService>();
        app.MapGrpcService<MfaSessionGrpcService>();
        app.MapGrpcService<UserSessionGrpcService>();
        app.MapGrpcService<LinkedProviderGrpcService>();

        return app;
    }

    private static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = entry.Value.Duration.TotalMilliseconds,
                    data = entry.Value.Data,
                },
                StringComparer.Ordinal),
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
