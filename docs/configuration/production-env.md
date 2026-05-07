# Production конфигурация

Production окружение должно передавать настройки через secret manager, CI/CD protected variables, orchestrator secrets или защищенные environment variables. Не используйте dev `.env` как источник production values.

## Обязательные группы настроек

- Gateway: `Jwt__SecretKey`, `Cors__AllowedOrigins__0`, `Services__Authentication`, `Services__ScenarioRepository`, `Services__BotManager`.
- User-authentication: `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`, `TotpEncryption__Key`, `ExternalProviders__Yandex__ClientId`, `ExternalProviders__Yandex__ClientSecret`, `ExternalProviders__Yandex__RedirectUri`.
- Scenario-repository: `ConnectionStrings__DefaultConnection`, `MongoDb__ConnectionString`, `MongoDb__DatabaseName`, `RabbitMq__*`, `InternalApi__AccessToken`, `BotManager__Address`, `BotManager__AccessToken`.
- Scenario-engine: `MongoDb__ConnectionString`, `MongoDb__DatabaseName`, `RabbitMq__*`, `BotManager__Address`, `BotManager__AccessToken`, `ScenarioRepository__Address`, `Vk__ApiVersion`.
- Bot-manager: `ConnectionStrings__DefaultConnection`, `RabbitMq__*`, `InternalApi__AccessToken`, `BotTokenEncryption__Key`, `ScenarioRepository__Address`, `ScenarioRepository__AccessToken`.
- Bot-integration: `RabbitMq__*`, `BotManager__Address`, `BotManager__AccessToken`, `Vk__ApiVersion`, `Vk__WaitSeconds`, `Telegram__TimeoutSeconds`, `Telegram__RetryDelaySeconds`.
- Notification: `RabbitMq__*`, `Email__Username`, `Email__Password`.
- Web-client: `VITE_API_URL`, `VITE_YANDEX_CLIENT_ID`.

## Production требования

- JWT secret и encryption keys должны быть достаточно длинными и случайными.
- Internal API tokens должны быть разными для разных внутренних API.
- SMTP credentials должны иметь минимально необходимый доступ.
- OAuth redirect URI должен указывать на production callback.
- CORS должен разрешать только production frontend origins.
- Monitoring endpoints должны быть закрыты сетевыми правилами или auth layer.
- Connection strings не должны логироваться.
- Bot tokens не должны попадать в logs, metrics или screenshots.

## Миграции

Миграции для `authentication`, `scenario-repository` и `bot-manager` в dev compose вынесены в отдельные migration targets. В production их следует запускать отдельным controlled job перед обновлением сервисов или как часть release pipeline.
