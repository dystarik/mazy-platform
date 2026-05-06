namespace MazyPlatform.Service.Gateway.Configuration;

using MazyPlatform.Service.Gateway.Interceptors;
using MazyPlatform.Service.Gateway.Proxies.Authentication;
using MazyPlatform.Service.Gateway.Proxies.BotManager;
using MazyPlatform.Service.Gateway.Proxies.ScenarioRepository;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Prometheus;
using Serilog;
using Serilog.Events;

internal static class WebApplicationExtensions
{
    public static WebApplication UseMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseSerilogRequestLogging(o =>
        {
            o.MessageTemplate = "Обработан {RequestMethod} {RequestPath}: {StatusCode} за {Elapsed:0.0000} мс";
            o.GetLevel = GetRequestLogLevel;
            o.EnrichDiagnosticContext = EnrichRequestLog;
        });
        app.UseHttpMetrics();
        app.UseRateLimiter();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGatewayHealthChecks();
        app.MapMetrics();

        if (app.Environment.IsDevelopment())
            app.UseSwagger();

        app.MapGrpcService<AuthenticationServiceProxy>().RequireAuthorization();
        app.MapGrpcService<MfaServiceProxy>().RequireAuthorization();
        app.MapGrpcService<MfaSessionServiceProxy>().RequireAuthorization();
        app.MapGrpcService<PasswordServiceProxy>().RequireAuthorization();
        app.MapGrpcService<RegistrationServiceProxy>().RequireAuthorization();
        app.MapGrpcService<UserSessionServiceProxy>().RequireAuthorization();
        app.MapGrpcService<LinkedProviderServiceProxy>().RequireAuthorization();

        app.MapGrpcService<ProjectsServiceProxy>().RequireAuthorization();
        app.MapGrpcService<EntitySchemasServiceProxy>().RequireAuthorization();
        app.MapGrpcService<ScenarioGraphsServiceProxy>().RequireAuthorization();
        app.MapGrpcService<UserDataServiceProxy>().RequireAuthorization();

        app.MapGrpcService<BotServiceProxy>().RequireAuthorization();

        return app;
    }

    private static void EnrichRequestLog(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("TraceId", GetTraceId(httpContext));
        diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
    }

    private static LogEventLevel GetRequestLogLevel(HttpContext httpContext, double elapsed, Exception? exception)
    {
        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        return IsTelemetryEndpoint(httpContext)
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
    }

    private static bool IsTelemetryEndpoint(HttpContext httpContext)
        => httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || httpContext.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase);

    private static string? GetTraceId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(TraceContext.ItemKey, out var traceId) && traceId is string value)
            return value;

        return httpContext.Request.Headers[TraceContext.HeaderName].FirstOrDefault();
    }

    private static void MapGatewayHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        }).AllowAnonymous();
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
