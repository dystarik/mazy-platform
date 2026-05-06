namespace MazyPlatform.Service.Bot.Manager.Api.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class BotManagerConfigurationHealthCheck(IConfiguration configuration, IWebHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new Dictionary<string, object>(StringComparer.Ordinal);

        ValidateRequiredValue(failures, "InternalApi:AccessToken");
        ValidateRequiredValue(failures, "ConnectionStrings:DefaultConnection");
        ValidateRabbitMq(failures);
        ValidateRequiredValue(failures, "BotTokenEncryption:Key");
        ValidateRequiredValue(failures, "ScenarioRepository:Address");
        ValidateRequiredValue(failures, "ScenarioRepository:AccessToken");

        return failures.Count == 0
            ? Task.FromResult(HealthCheckResult.Healthy("Конфигурация менеджера ботов готова."))
            : Task.FromResult(HealthCheckResult.Unhealthy("Конфигурация менеджера ботов не готова к production.", data: failures));
    }

    private void ValidateRabbitMq(Dictionary<string, object> failures)
    {
        ValidateRequiredValue(failures, "RabbitMq:Host");
        ValidateRequiredValue(failures, "RabbitMq:Port");
        ValidateRequiredValue(failures, "RabbitMq:Username");
        ValidateRequiredValue(failures, "RabbitMq:Password");
        ValidateRequiredValue(failures, "RabbitMq:VirtualHost");
        ValidateRequiredValue(failures, "RabbitMq:ExchangeName");
        ValidateRequiredValue(failures, "RabbitMq:ExchangeType");
        ValidateRequiredValue(failures, "RabbitMq:DeadLetterExchangeName");
        ValidateRequiredValue(failures, "RabbitMq:Prefetch");
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
