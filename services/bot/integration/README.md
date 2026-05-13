# bot-integration

Worker интеграции с внешними bot providers. Он синхронизирует active bots из bot-manager, запускает VK/Telegram polling и публикует входящие события в RabbitMQ для scenario-engine.

## Где смотреть код

- `src/MazyPlatform.Service.Bot.Integration/Program.cs` - DI, hosted services, RabbitMQ, metrics.
- `LongPoll` - polling внешних платформ.
- `Grpc` - client к internal API bot-manager.
- `Caching` - локальное состояние active bots.
- `Observability` - diagnostics/config checks.
- `tests/MazyPlatform.Service.Bot.Integration.Integration.Tests` - initial sync, queue behavior, robustness.
- Контракты событий лежат в `libraries/contracts`, runtime incoming event types - в `libraries/scenario`.

## Запуск и отладка

```powershell
.\tools\start.cmd
```

Для локального debug выберите `debug.bot.integration.yml` и запускайте:

```powershell
dotnet run --project services\bot\integration\src\MazyPlatform.Service.Bot.Integration\MazyPlatform.Service.Bot.Integration.csproj
```

Compose оставляет RabbitMQ и bot-manager доступными. В compose worker ходит к `bot-manager:5102`.

Важные настройки по именам:

- `RabbitMq__*`
- `BotManager__Address`, `BotManager__AccessToken`
- `Vk__ApiVersion`, `Vk__WaitSeconds`
- `Telegram__TimeoutSeconds`, `Telegram__RetryDelaySeconds`

## Что проверять

```powershell
dotnet test services\bot\integration\MazyPlatform.Service.Bot.Integration.slnx
```

Ручная проверка:

- initial sync active bots после старта;
- queues `bot-integration.*` и DLQ в RabbitMQ;
- реакция на события activate/deactivate/delete/token/version/unbind;
- публикация `bot.integration.incoming_event`;
- отсутствие plaintext bot token в logs;
- metrics на `MetricsPort`.

## События

Слушает события:

- `bot.manager.bot-instance-activated`
- `bot.manager.bot-instance-deactivated`
- `bot.manager.bot-instance-deleted`
- `bot.manager.bot-instance-token-changed`
- `bot.manager.bot-instance-unbound-from-project`
- `bot.manager.bot-instance-scenario-version-changed`

Публикует:

- `bot.integration.incoming_event`

## Рискованные изменения

- Polling loop должен корректно останавливаться при deactivate/delete/token change.
- Ошибки внешних API не должны валить весь worker или заспамить retry без паузы.
- Incoming event payload должен оставаться совместимым со scenario-engine.
- Access token к bot-manager нужен только для internal gRPC; не выводите его в logs.
- Дубликаты incoming events возможны при сетевых сбоях: меняя retry, проверяйте idempotency downstream.
