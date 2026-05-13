# bot-manager

Сервис управляет bot instances: создание, привязка к project, activation/deactivation, обновление token, удаление, выбор scenario version и auto update. Публичный API идет через gateway; internal API отдает credentials и active bots workers.

## Где смотреть код

- `src/MazyPlatform.Service.Bot.Manager.Api` - public/internal gRPC services, interceptors, appsettings.
- `src/MazyPlatform.Service.Bot.Manager.Application` - use cases lifecycle, binding, activation, scenario version.
- `src/MazyPlatform.Service.Bot.Manager.Domain` - BotInstance и инварианты состояния.
- `src/MazyPlatform.Service.Bot.Manager.Infrastructure` - PostgreSQL, RabbitMQ, encryption, scenario-repository client.
- `tests/MazyPlatform.Service.Bot.Manager.Domain.Tests` - доменная модель.
- `tests/MazyPlatform.Service.Bot.Manager.Integration.Tests` - API, events, internal API, token safety, consistency.

## Запуск и отладка

```powershell
.\tools\start.cmd
```

Отдельного debug overlay в `docs/local-development.md` для bot-manager нет. Для локального `dotnet run` оставьте PostgreSQL/RabbitMQ/scenario-repository в compose и задайте настройки окружения вручную:

```powershell
dotnet run --project services\bot\manager\src\MazyPlatform.Service.Bot.Manager.Api\MazyPlatform.Service.Bot.Manager.Api.csproj
```

Public gRPC endpoint: `5101`, internal: `5102`. В compose gateway ходит на public endpoint, workers - на internal endpoint.

Важные настройки по именам:

- `ConnectionStrings__DefaultConnection`
- `RabbitMq__*`
- `InternalApi__AccessToken`
- `BotTokenEncryption__Key`
- `ScenarioRepository__Address`, `ScenarioRepository__AccessToken`

## Что проверять

```powershell
dotnet test services\bot\manager\MazyPlatform.Service.Bot.Manager.slnx
```

Ручной smoke:

- create/list/get bot через frontend/gateway;
- bind/unbind project и проверка доступа по user id;
- activate/deactivate/delete;
- update token без утечки token value в responses/logs;
- change scenario version и auto update при release events;
- internal credentials API доступен только с `InternalApi__AccessToken`.

## События

Слушает события:

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

## Рискованные изменения

- Bot token encryption: не логировать plaintext, не менять формат без миграции.
- Internal API: утечка credentials критична; endpoint не должен быть публичным.
- Scenario version switching влияет на scenario-engine и bot-integration через events.
- Project delete/release removed должны оставлять bots в согласованном состоянии.
- Access boundary по user/project проверяйте integration tests, не только happy path.
