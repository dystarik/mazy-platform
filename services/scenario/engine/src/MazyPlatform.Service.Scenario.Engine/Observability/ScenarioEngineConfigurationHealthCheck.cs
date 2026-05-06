namespace MazyPlatform.Service.Scenario.Engine.Observability;

using Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class ScenarioEngineConfigurationHealthCheck(
    IConfiguration configuration,
    IHostEnvironment environment) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new Dictionary<string, object>(StringComparer.Ordinal);

        ValidateRabbitMq(failures);
        ValidateRequiredValue(failures, "MongoDb:ConnectionString");
        ValidateRequiredValue(failures, "MongoDb:DatabaseName");
        ValidateRequiredValue(failures, "BotManager:Address");
        ValidateRequiredValue(failures, "BotManager:AccessToken");
        ValidateRequiredValue(failures, "ScenarioRepository:Address");
        ValidateRequiredValue(failures, "Vk:ApiVersion");

        return failures.Count == 0
            ? Task.FromResult(HealthCheckResult.Healthy("Конфигурация scenario-engine готова."))
            : Task.FromResult(HealthCheckResult.Unhealthy("Конфигурация scenario-engine не готова к production.", data: failures));
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

        if (!environment.IsDevelopment() && IsPlaceholder(value))
            failures[key] = "Значение конфигурации должно быть переопределено вне Development.";
    }

    private bool IsPlaceholder(string value)
    {
        return string.Equals(value, "REPLACE_ME", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "change-me", StringComparison.OrdinalIgnoreCase);
    }
}
