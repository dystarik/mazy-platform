namespace MazyPlatform.Service.Scenario.Repository.Api.Configuration;

using System.Globalization;

using MazyPlatform.Service.Scenario.Repository.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

internal static class WebApplicationExtensions
{
    public static WebApplication UseMiddleware(this WebApplication app)
    {
        var metricsPort = app.Configuration.GetValue<int>("MetricsPort");
        if (metricsPort > 0)
        {
            var metricServer = new KestrelMetricServer(metricsPort);
            metricServer.Start();
        }

        var publicHost = GetEndpointHost(app.Configuration, "Public", 0, 6101);
        var internalHost = GetEndpointHost(app.Configuration, "Internal", 1, 6102);

        app.UseForwardedHeaders();
        app.UseHttpMetrics();
        app.UseGrpcMetrics();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        }).RequireHost(publicHost);
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        }).RequireHost(publicHost);

        app.MapGrpcService<ProjectsGrpcService>().RequireHost(publicHost);
        app.MapGrpcService<EntitySchemasGrpcService>().RequireHost(publicHost);
        app.MapGrpcService<ScenarioGraphsGrpcService>().RequireHost(publicHost);
        app.MapGrpcService<UserDataGrpcService>().RequireHost(publicHost);
        app.MapGrpcService<ScenarioRepositoryInternalGrpcService>().RequireHost(internalHost);

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

    private static string GetEndpointHost(
        IConfiguration configuration,
        string endpointName,
        int urlIndex,
        int fallbackPort)
    {
        var endpointUrl = configuration[$"Kestrel:Endpoints:{endpointName}:Url"];
        if (TryGetPort(endpointUrl, out var endpointPort))
        {
            return string.Create(CultureInfo.InvariantCulture, $"*:{endpointPort}");
        }

        var urls = configuration["urls"]
            ?? configuration["URLS"]
            ?? configuration["ASPNETCORE_URLS"];

        if (!string.IsNullOrWhiteSpace(urls))
        {
            var configuredUrls = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (configuredUrls.Length > urlIndex && TryGetPort(configuredUrls[urlIndex], out var configuredPort))
            {
                return $"*:{configuredPort}";
            }
        }

        return $"*:{fallbackPort}";
    }

    private static bool TryGetPort(string? url, out int port)
    {
        port = 0;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var normalizedUrl = url
            .Replace("://+:", "://localhost:", StringComparison.Ordinal)
            .Replace("://*:", "://localhost:", StringComparison.Ordinal);

        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        port = uri.Port;
        return port > 0;
    }
}
