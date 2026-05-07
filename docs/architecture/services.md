# Backend-сервисы

## Gateway

Путь: `edge/gateway`.

Ответственность:

- публичный API для frontend;
- gRPC JSON transcoding в HTTP `/api/v1/*`;
- JWT validation и authorization;
- CORS и rate limiting;
- Swagger;
- propagation `x-trace-id`, user id, refresh token id и client IP в gRPC metadata.

API:

- проксирует user APIs: `AuthenticationService`, `RegistrationService`, `PasswordService`, `MfaService`, `MfaSessionService`, `UserSessionService`, `LinkedProviderService`;
- проксирует scenario APIs: `ProjectService`, `EntitySchemaService`, `ScenarioGraphService`, `UserDataService`;
- проксирует bot APIs: `BotService`;
- health: `/health/live`, `/health/ready`;
- metrics: `/metrics`.

Связи: вызывает `authentication`, `scenario-repository`, `bot-manager` по gRPC.

Зависимости: JWT secret, service addresses, CORS origins, Data Protection keys volume.

## User-authentication

Путь: `services/user/authentication`.

Ответственность: аккаунты пользователей, registration, login, external provider login, sessions, refresh/logout, password flows, Email MFA, TOTP MFA.

API/consumers:

- gRPC services: `RegistrationService`, `AuthenticationService`, `PasswordService`, `MfaService`, `MfaSessionService`, `UserSessionService`, `LinkedProviderService`;
- публикует RabbitMQ events: `user.authentication.registered`, `user.authentication.email-confirmed`, `user.authentication.mfa-email-code-generated`, `user.authentication.password-reset-requested`;
- consumers нет.

Связи: PostgreSQL, RabbitMQ, external provider Yandex, notification через events.

Зависимости: `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, `RabbitMq__*`, `TotpEncryption__Key`, `ExternalProviders__Yandex__*`.

## Scenario-repository

Путь: `services/scenario/repository`.

Ответственность: проекты, entity schemas, draft scenario graph, validation, release promotion, rollback, version history, delete version, internal validation for bot binding.

API/consumers:

- public gRPC: `ProjectService`, `EntitySchemaService`, `ScenarioGraphService`, `UserDataService`;
- internal gRPC: `ScenarioRepositoryInternalService`;
- consumers: `user.authentication.email-confirmed`;
- публикует events: `scenario.repository.project-deleted`, `scenario.repository.scenario-release-changed`, `scenario.repository.scenario-release-removed`, `scenario.repository.scenario-version-deleted`.

Связи: PostgreSQL, MongoDB, RabbitMQ, `bot-manager` internal gRPC для проверок использования версий.

Зависимости: `ConnectionStrings__DefaultConnection`, `MongoDb__*`, `RabbitMq__*`, `InternalApi__AccessToken`, `BotManager__*`.

## Scenario-engine

Путь: `services/scenario/engine`.

Ответственность: исполнение опубликованных сценариев по входящим bot events, синхронизация активных ботов, отправка действий во внешние платформы через bot credentials.

API/consumers:

- public HTTP API нет, это worker;
- consumers: `bot.integration.incoming_event`, `bot.manager.bot-instance-activated`, `bot.manager.bot-instance-deactivated`, `bot.manager.bot-instance-deleted`, `bot.manager.bot-instance-token-changed`, `bot.manager.bot-instance-unbound-from-project`, `bot.manager.bot-instance-scenario-version-changed`;
- gRPC clients: `BotInternalService`, `ScenarioGraphService`.

Связи: RabbitMQ, MongoDB, `bot-manager`, `scenario-repository`, VK API.

Зависимости: `RabbitMq__*`, `MongoDb__*`, `Vk__ApiVersion`, `BotManager__*`, `ScenarioRepository__Address`, `DelayResume__*`.

## Bot-manager

Путь: `services/bot/manager`.

Ответственность: lifecycle ботов, credentials, project binding, activation/deactivation, scenario version switching, internal credentials API для workers.

API/consumers:

- public gRPC: `BotService`;
- internal gRPC: `BotInternalService`;
- consumers: `user.authentication.email-confirmed`, `scenario.repository.project-deleted`, `scenario.repository.scenario-release-changed`, `scenario.repository.scenario-release-removed`;
- публикует events: `bot.manager.bot-instance-created`, `bot.manager.bot-instance-activated`, `bot.manager.bot-instance-deactivated`, `bot.manager.bot-instance-deleted`, `bot.manager.bot-instance-token-changed`, `bot.manager.bot-instance-unbound-from-project`, `bot.manager.bot-instance-scenario-version-changed`.

Связи: PostgreSQL, RabbitMQ, `scenario-repository` internal gRPC.

Зависимости: `ConnectionStrings__DefaultConnection`, `RabbitMq__*`, `InternalApi__AccessToken`, `BotTokenEncryption__Key`, `ScenarioRepository__*`.

## Bot-integration

Путь: `services/bot/integration`.

Ответственность: long polling внешних bot providers, синхронизация активных ботов, публикация входящих событий в RabbitMQ.

API/consumers:

- public HTTP/gRPC API нет, это worker;
- consumers: bot-manager lifecycle events;
- publishes: `bot.integration.incoming_event`;
- gRPC client: `BotInternalService`.

Связи: RabbitMQ, `bot-manager`, VK API, Telegram API.

Зависимости: `RabbitMq__*`, `BotManager__*`, `Vk__*`, `Telegram__*`.

## Notification

Путь: `services/notification`.

Ответственность: отправка email-уведомлений по user authentication events.

API/consumers:

- public HTTP/gRPC API нет, это worker;
- consumers: `user.authentication.registered`, `user.authentication.mfa-email-code-generated`, `user.authentication.password-reset-requested`;
- RabbitMQ queue подписана на `user.#`.

Связи: RabbitMQ, SMTP/email provider.

Зависимости: `RabbitMq__*`, `Email__Username`, `Email__Password`.
