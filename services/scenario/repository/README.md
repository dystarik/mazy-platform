# scenario-repository

Сервис владеет проектами, entity schemas, draft graph, validation, release versions, rollback/version history и user data records. Public gRPC идет через gateway; internal gRPC используется bot-manager для проверок binding/version usage.

## Где смотреть код

- `src/MazyPlatform.Service.Scenario.Repository.Api` - public/internal gRPC services, endpoints `6101/6102`, appsettings.
- `src/MazyPlatform.Service.Scenario.Repository.Application` - use cases проектов, schemas, graph, release, user data.
- `src/MazyPlatform.Service.Scenario.Repository.Domain` - Project, ScenarioGraph, ScenarioVersion и specifications validation.
- `src/MazyPlatform.Service.Scenario.Repository.Infrastructure` - PostgreSQL, MongoDB runtime documents, RabbitMQ, internal clients.
- `tests/MazyPlatform.Service.Scenario.Repository.Integration.Tests` - основной safety net для repository behavior.
- `libraries/scenario` - catalog/validation/runtime модели, которые сервис использует.

## Запуск и отладка

```powershell
.\tools\start.cmd
```

Для локального API выберите `debug.scenario.repository.yml`, оставьте PostgreSQL/MongoDB/RabbitMQ и соседние сервисы в compose, затем запускайте:

```powershell
dotnet run --project services\scenario\repository\src\MazyPlatform.Service.Scenario.Repository.Api\MazyPlatform.Service.Scenario.Repository.Api.csproj
```

Public gRPC endpoint в appsettings: `6101`, internal: `6102`. Internal endpoint должен быть доступен только сервисам внутри backend-сети.

Важные настройки по именам:

- `ConnectionStrings__DefaultConnection`
- `MongoDb__ConnectionString`, `MongoDb__DatabaseName`
- `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`
- `InternalApi__AccessToken`
- `BotManager__Address`, `BotManager__AccessToken`

## Что проверять

```powershell
dotnet test services\scenario\repository\MazyPlatform.Service.Scenario.Repository.slnx
```

Ручной smoke через frontend/gateway:

- пользователь с `email-confirmed` видит/создает проекты;
- create/delete project публикует ожидаемые events;
- schemas CRUD не ломает user data records;
- draft save -> validate -> promote/release -> rollback/version history;
- delete scenario version учитывает проверки использования в bot-manager;
- node catalog совпадает с editor expectations.

Health endpoints: `/health/live`, `/health/ready`.

## События

Consumer:

- `user.authentication.email-confirmed`

Публикует:

- `scenario.repository.project-deleted`
- `scenario.repository.scenario-release-changed`
- `scenario.repository.scenario-release-removed`
- `scenario.repository.scenario-version-deleted`

## Рискованные изменения

- Graph/version schema хранится в PostgreSQL и MongoDB: миграции и runtime documents должны оставаться совместимыми.
- Validation должна совпадать с frontend editor и `libraries/scenario`.
- Release events двигают bot-manager и scenario-engine; проверяйте routing keys и payload.
- Internal API auth token нельзя обходить ради тестов.
- Delete/rollback version опасны для активных bots; проверяйте fake/real bot-manager integration tests.
