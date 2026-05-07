# Настройки сервисов

## Gateway

- `Jwt__SecretKey` - проверка JWT, secret.
- `Cors__AllowedOrigins__0` - разрешенный frontend origin.
- `Services__Authentication` - gRPC address user-authentication.
- `Services__ScenarioRepository` - gRPC address scenario-repository.
- `Services__BotManager` - gRPC address bot-manager.

## User-authentication

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection, secret-bearing.
- `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password` - RabbitMQ connection, password is secret.
- `RabbitMq__ExchangeName` - exchange для user events.
- `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` - JWT settings.
- `TotpEncryption__Key` - encryption key, secret.
- `Totp__Issuer`, `Totp__VerificationWindowPastSteps`, `Totp__VerificationWindowFutureSteps` - TOTP behavior.
- `ExternalProviders__Yandex__ClientId`, `ExternalProviders__Yandex__ClientSecret`, `ExternalProviders__Yandex__RedirectUri` - Yandex provider settings, client secret is secret.

## Scenario-repository

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection, secret-bearing.
- `MongoDb__ConnectionString`, `MongoDb__DatabaseName` - MongoDB storage, connection string is secret-bearing.
- `RabbitMq__*` - exchange, queues, prefetch, DLQ.
- `InternalApi__AccessToken` - token для internal gRPC, secret.
- `BotManager__Address`, `BotManager__AccessToken` - internal bot-manager client, token is secret.

## Scenario-engine

- `RabbitMq__*` - queues for bot incoming and bot lifecycle events.
- `MongoDb__ConnectionString`, `MongoDb__DatabaseName` - scenario runtime storage.
- `DelayResume__Enabled`, `DelayResume__PollIntervalSeconds`, `DelayResume__BatchSize`, `DelayResume__LockSeconds` - delayed execution resume.
- `Vk__ApiVersion` - VK API version.
- `BotManager__Address`, `BotManager__AccessToken` - credentials API, token is secret.
- `ScenarioRepository__Address` - scenario graph API.

## Bot-manager

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection, secret-bearing.
- `RabbitMq__*` - bot lifecycle exchange/queues.
- `InternalApi__AccessToken` - internal API token, secret.
- `BotTokenEncryption__Key` - encryption key, secret.
- `ScenarioRepository__Address`, `ScenarioRepository__AccessToken` - internal validation API, token is secret.

## Bot-integration

- `RabbitMq__*` - lifecycle queues and incoming event publishing.
- `BotManager__Address`, `BotManager__AccessToken` - active bot/credentials API, token is secret.
- `Vk__ApiVersion`, `Vk__WaitSeconds` - VK long polling.
- `Telegram__TimeoutSeconds`, `Telegram__RetryDelaySeconds` - Telegram polling/retry behavior.

## Notification

- `RabbitMq__*` - queue subscription to user events.
- `Email__Username`, `Email__Password` - email provider credentials, password is secret.

## Web-client

- `VITE_API_URL` - API base URL.
- `VITE_YANDEX_CLIENT_ID` - public provider client id.

## Tools packages

- `WORKSPACE_ROOT`, `LIBRARIES_ROOT`, `LIBRARIES`, `SYNC_PACKAGE_ROOTS`, `UPDATE_PACKAGE_ROOTS` - paths and package roots.
- `NUGET_SOURCE` - package source.
- `NUGET_API_KEY` - package registry secret.
