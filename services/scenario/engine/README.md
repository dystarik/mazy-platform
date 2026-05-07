# scenario-engine

Worker исполнения сценариев. Получает входящие события от ботов, загружает нужную release-версию сценария, исполняет graph и отправляет действия во внешнюю платформу.

## API/consumers

Public API отсутствует.

Consumers:

- `bot.integration.incoming_event`
- `bot.manager.bot-instance-activated`
- `bot.manager.bot-instance-deactivated`
- `bot.manager.bot-instance-deleted`
- `bot.manager.bot-instance-token-changed`
- `bot.manager.bot-instance-unbound-from-project`
- `bot.manager.bot-instance-scenario-version-changed`

gRPC clients:

- `BotInternalService` для credentials и active bots.
- `ScenarioGraphService` для release/version graph.

## Зависимости

- RabbitMQ.
- MongoDB scenario runtime storage.
- `bot-manager`.
- `scenario-repository`.
- VK API; Telegram support зависит от platform adapters.
- `libraries/scenario`.

## Env/settings

- `RabbitMq__*`
- `MongoDb__ConnectionString`, `MongoDb__DatabaseName`
- `DelayResume__Enabled`, `DelayResume__PollIntervalSeconds`, `DelayResume__BatchSize`, `DelayResume__LockSeconds`
- `Vk__ApiVersion`
- `BotManager__Address`, `BotManager__AccessToken`
- `ScenarioRepository__Address`

## Health/ready/metrics

Это worker service. Состояние проверяется по process health, logs и metrics на `MetricsPort`. В compose metrics собирает Prometheus.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```

Для отладки выберите `debug.scenario.engine.yml`.
