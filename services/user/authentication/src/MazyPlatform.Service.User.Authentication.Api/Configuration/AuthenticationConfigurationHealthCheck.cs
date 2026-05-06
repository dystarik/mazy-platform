namespace MazyPlatform.Service.User.Authentication.Api.Configuration;

using System.Text;

using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class AuthenticationConfigurationHealthCheck(
    IConfiguration configuration,
    IWebHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new Dictionary<string, object>(StringComparer.Ordinal);

        ValidateRequiredValue(failures, "ConnectionStrings:DefaultConnection");
        ValidateJwt(failures);
        ValidateRabbitMq(failures);
        ValidateRequiredValue(failures, "TotpEncryption:Key");
        ValidateRequiredValue(failures, "Totp:Issuer");
        ValidateYandexProvider(failures);

        return failures.Count == 0
            ? Task.FromResult(HealthCheckResult.Healthy("Конфигурация сервиса аутентификации готова."))
            : Task.FromResult(HealthCheckResult.Unhealthy("Конфигурация сервиса аутентификации не готова к production.", data: failures));
    }

    private void ValidateJwt(Dictionary<string, object> failures)
    {
        ValidateRequiredValue(failures, "Jwt:Issuer");
        ValidateRequiredValue(failures, "Jwt:Audience");
        ValidateRequiredValue(failures, "Jwt:ExpirationMinutes");

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

    private void ValidateRabbitMq(Dictionary<string, object> failures)
    {
        ValidateRequiredValue(failures, "RabbitMq:Host");
        ValidateRequiredValue(failures, "RabbitMq:Port");
        ValidateRequiredValue(failures, "RabbitMq:Username");
        ValidateRequiredValue(failures, "RabbitMq:Password");
        ValidateRequiredValue(failures, "RabbitMq:VirtualHost");
        ValidateRequiredValue(failures, "RabbitMq:ExchangeName");
    }

    private void ValidateYandexProvider(Dictionary<string, object> failures)
    {
        ValidateRequiredValue(failures, "ExternalProviders:Yandex:ClientId");
        ValidateRequiredValue(failures, "ExternalProviders:Yandex:ClientSecret");
        ValidateRequiredValue(failures, "ExternalProviders:Yandex:RedirectUri");
    }

    private void ValidateRequiredValue(Dictionary<string, object> failures, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            failures[key] = "Значение конфигурации обязательно.";
            return;
        }

        if (!environment.IsDevelopment() && string.Equals(value, "REPLACE_ME", StringComparison.OrdinalIgnoreCase))
            failures[key] = "Значение конфигурации должно быть переопределено вне Development.";
    }
}
