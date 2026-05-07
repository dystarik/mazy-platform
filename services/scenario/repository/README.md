# scenario-repository

Сервис владеет проектами, entity schemas, draft graph, release versions, validation, rollback/version history и user data records.

## gRPC/HTTP API

Public services:

- `ProjectService` - create/delete/get/list projects.
- `EntitySchemaService` - schemas внутри project.
- `ScenarioGraphService` - draft, validate, promote, rollback, release, version history, delete version, node catalog.
- `UserDataService` - records пользовательских данных.

Internal service:

- `ScenarioRepositoryInternalService` - validation project/version для bot-manager.

## RabbitMQ

Consumers:

- `user.authentication.email-confirmed`.

Публикует:

- `scenario.repository.project-deleted`
- `scenario.repository.scenario-release-changed`
- `scenario.repository.scenario-release-removed`
- `scenario.repository.scenario-version-deleted`

## Зависимости

- PostgreSQL для metadata.
- MongoDB для graph/runtime documents.
- RabbitMQ.
- `bot-manager` internal gRPC для проверки использования версий.
- `libraries/scenario` для catalog/validation.

## Env/settings

- `ConnectionStrings__DefaultConnection`
- `MongoDb__ConnectionString`, `MongoDb__DatabaseName`
- `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`, queues/exchange settings
- `InternalApi__AccessToken`
- `BotManager__Address`, `BotManager__AccessToken`

## Health/ready/metrics

- `/health/live`
- `/health/ready`
- metrics endpoint для Prometheus.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```

Для отладки выберите `debug.scenario.repository.yml` и запускайте сервис локально с зависимостями в compose.
