namespace MazyPlatform.Service.Bot.Manager.Api.Configuration;

using System.Globalization;
using System.Text.Json;

using MazyPlatform.Service.Bot.Manager.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

internal static class WebApplicationExtensions
{
    public static WebApplication UseMiddleware(this WebApplication app)
    {
        var publicHost = GetEndpointHost(app.Configuration, "Public", 0, 5101);
        var internalHost = GetEndpointHost(app.Configuration, "Internal", 1, 5102);
        var metricsPort = app.Configuration.GetValue<int?>("MetricsPort");

        if (metricsPort is > 0)
            new KestrelMetricServer(metricsPort.Value).Start();

        app.UseForwardedHeaders();
        app.UseHttpMetrics();
        app.UseGrpcMetrics();
        app.MapGrpcService<BotGrpcService>().RequireHost(publicHost);
        app.MapGrpcService<BotInternalGrpcService>().RequireHost(internalHost);
        app.MapHealthChecks("/health/live", CreateHealthCheckOptions("live")).RequireHost(publicHost);
        app.MapHealthChecks("/health/ready", CreateHealthCheckOptions("ready")).RequireHost(publicHost);
        return app;
    }

    private static HealthCheckOptions CreateHealthCheckOptions(string tag) => new()
    {
        Predicate = check => check.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    data = e.Value.Data,
                }),
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted);
        },
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
        },
    };

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
