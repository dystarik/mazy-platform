namespace MazyPlatform.Service.Notification.Observability;

using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

internal sealed partial class DiagnosticsHostedService(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DiagnosticsHostedService> logger) : IHostedService, IAsyncDisposable
{
    private WebApplication? _app;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = configuration.GetValue<int?>("MetricsPort") ?? 9090;
        if (port <= 0)
        {
            LogDisabled();
            return;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(DiagnosticsHostedService).Assembly.FullName,
        });

        builder.Configuration.AddConfiguration(configuration);
        builder.WebHost.UseKestrel().UseUrls($"http://0.0.0.0:{port}");
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Notification service запущен."), tags: ["live"])
            .AddCheck("configuration", new NotificationConfigurationHealthCheck(configuration, environment), tags: ["ready"]);

        _app = builder.Build();
        _app.MapMetrics();
        _app.MapHealthChecks("/health/live", CreateHealthCheckOptions("live"));
        _app.MapHealthChecks("/health/ready", CreateHealthCheckOptions("ready"));

        await _app.StartAsync(cancellationToken);
        LogStarted(port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
            await _app.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
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

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Diagnostics endpoint отключен.")]
    private partial void LogDisabled();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Diagnostics endpoint запущен. Port: {Port}.")]
    private partial void LogStarted(int port);
}
