# scenario-engine

Worker исполнения опубликованных сценариев. Он получает входящие события от bot-integration, синхронизирует состояние активных ботов, загружает release graph и отправляет действия во внешние платформы через credentials из bot-manager.

## Где смотреть код

- `src/MazyPlatform.Service.Scenario.Engine/Program.cs` - DI, hosted services, messaging, metrics.
- `Startup/BotsInitialSyncService.cs` - первичная синхронизация active bots.
- `Scenarios/ScenarioLoader.cs` - загрузка graph/version из scenario-repository.
- `Platforms` - маппинг platform adapters.
- `Observability` - health/config diagnostics.
- `tests/MazyPlatform.Service.Scenario.Engine.Integration.Tests` - RabbitMQ, cache, delay resume, graph execution, poison/robustness tests.
- `libraries/scenario` - executor, nodes, validation, Mongo storage.

## Запуск и отладка

Обычный запуск:

```powershell
.\tools\start.cmd
```

Для debug выберите `debug.scenario.engine.yml` и запускайте worker локально:

```powershell
dotnet run --project services\scenario\engine\src\MazyPlatform.Service.Scenario.Engine\MazyPlatform.Service.Scenario.Engine.csproj
```

Compose оставляет RabbitMQ, MongoDB, bot-manager и scenario-repository доступными. В compose worker ходит к `bot-manager:5102` и `scenario-repository:6101`.

Важные настройки по именам:

- `RabbitMq__*`
- `MongoDb__ConnectionString`, `MongoDb__DatabaseName`
- `DelayResume__Enabled`, `DelayResume__PollIntervalSeconds`, `DelayResume__BatchSize`, `DelayResume__LockSeconds`
- `Vk__ApiVersion`
- `BotManager__Address`, `BotManager__AccessToken`
- `ScenarioRepository__Address`

## Что проверять

```powershell
dotnet test services\scenario\engine\MazyPlatform.Service.Scenario.Engine.slnx
```

При ручной проверке смотрите:

- queues `scenario-engine.*` в RabbitMQ и DLQ;
- initial sync active bots после старта;
- реакцию на события activate/deactivate/delete/token/version/unbind;
- исполнение incoming event -> outgoing platform action;
- delay resume после рестарта worker;
- logs/metrics на `MetricsPort` через Prometheus/Grafana.

## События и клиенты

Слушает события:

- `bot.integration.incoming_event`
- `bot.manager.bot-instance-activated`
- `bot.manager.bot-instance-deactivated`
- `bot.manager.bot-instance-deleted`
- `bot.manager.bot-instance-token-changed`
- `bot.manager.bot-instance-unbound-from-project`
- `bot.manager.bot-instance-scenario-version-changed`

gRPC clients: `BotInternalService`, `ScenarioGraphService`.

## Рискованные изменения

- Cache active bots/credentials: stale state приводит к отправке действий не тем токеном или не той версией scenario.
- Delay/resume и Mongo session state: проверяйте поведение после рестартов и повторной доставки RabbitMQ.
- Runtime changes в `libraries/scenario` должны исполнять уже опубликованные release graphs.
- Poison messages не должны блокировать очередь навсегда.
- Platform adapters должны сохранять общий contract outgoing actions.
