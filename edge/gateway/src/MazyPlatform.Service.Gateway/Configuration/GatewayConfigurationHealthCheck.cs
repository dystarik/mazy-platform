namespace MazyPlatform.Service.Gateway.Configuration;

using System.Text;

using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class GatewayConfigurationHealthCheck(
    IConfiguration configuration,
    IWebHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new Dictionary<string, object>(StringComparer.Ordinal);

        ValidateRequiredValue(failures, "Jwt:Issuer");
        ValidateRequiredValue(failures, "Jwt:Audience");
        ValidateJwtSecret(failures);

        ValidateRequiredUri(failures, "Services:Authentication");
        ValidateRequiredUri(failures, "Services:ScenarioRepository");
        ValidateRequiredUri(failures, "Services:BotManager");

        if (!environment.IsDevelopment())
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (allowedOrigins is not { Length: > 0 })
                failures["Cors:AllowedOrigins"] = "Вне Development должен быть задан хотя бы один разрешенный origin.";
        }

        return failures.Count == 0
            ? Task.FromResult(HealthCheckResult.Healthy("Конфигурация gateway готова."))
            : Task.FromResult(HealthCheckResult.Unhealthy("Конфигурация gateway не готова к production.", data: failures));
    }

    private void ValidateJwtSecret(Dictionary<string, object> failures)
    {
        var secretKey = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            failures["Jwt:SecretKey"] = "Ключ подписи JWT обязателен.";
            return;
        }

        if (environment.IsDevelopment())
            return;

        if (string.Equals(secretKey, "REPLACE_ME", StringComparison.OrdinalIgnoreCase))
        {
            failures["Jwt:SecretKey"] = "Ключ подписи JWT должен быть переопределен вне Development.";
            return;
        }

        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
            failures["Jwt:SecretKey"] = "Ключ подписи JWT должен быть не короче 32 байт для production.";
    }

    private void ValidateRequiredValue(Dictionary<string, object> failures, string key)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
            failures[key] = "Значение конфигурации обязательно.";
    }

    private void ValidateRequiredUri(Dictionary<string, object> failures, string key)
    {
        var value = configuration[key];
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            failures[key] = "Требуется абсолютный URI сервиса.";
    }
}
