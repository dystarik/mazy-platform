# bot-manager

Сервис управляет bot instances: создание, binding к project, activation/deactivation, token update, удаление, выбор scenario version и режим auto update.

## gRPC/HTTP API

Public service:

- `BotService` - create, get, list, bind/unbind, activate/deactivate, update token, change scenario version, change update mode, delete.

Internal service:

- `BotInternalService` - active bots, credentials, проверка использования scenario version.

## RabbitMQ

Consumers:

- `user.authentication.email-confirmed`
- `scenario.repository.project-deleted`
- `scenario.repository.scenario-release-changed`
- `scenario.repository.scenario-release-removed`

Публикует:

- `bot.manager.bot-instance-created`
- `bot.manager.bot-instance-activated`
- `bot.manager.bot-instance-deactivated`
- `bot.manager.bot-instance-deleted`
- `bot.manager.bot-instance-token-changed`
- `bot.manager.bot-instance-unbound-from-project`
- `bot.manager.bot-instance-scenario-version-changed`

## Зависимости

- PostgreSQL.
- RabbitMQ.
- `scenario-repository` internal gRPC.
- Encryption key для bot tokens.

## Env/settings

- `ConnectionStrings__DefaultConnection`
- `RabbitMq__*`
- `InternalApi__AccessToken`
- `BotTokenEncryption__Key`
- `ScenarioRepository__Address`, `ScenarioRepository__AccessToken`

## Health/ready/metrics

- `/health/live`
- `/health/ready`
- metrics endpoint для Prometheus.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```

Сервис имеет public и internal gRPC endpoints. Internal endpoint должен оставаться недоступным извне production-сети.
