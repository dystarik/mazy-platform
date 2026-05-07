# bot-integration

Worker для интеграции с внешними bot providers. Он синхронизирует активных ботов из bot-manager, запускает VK/Telegram polling и публикует входящие события в RabbitMQ.

## API/consumers

Public API отсутствует.

Consumers:

- `bot.manager.bot-instance-activated`
- `bot.manager.bot-instance-deactivated`
- `bot.manager.bot-instance-deleted`
- `bot.manager.bot-instance-token-changed`
- `bot.manager.bot-instance-unbound-from-project`
- `bot.manager.bot-instance-scenario-version-changed`

Публикует:

- `bot.integration.incoming_event`

gRPC clients:

- `BotInternalService` для active bots и credentials.

## Зависимости

- RabbitMQ.
- `bot-manager` internal gRPC.
- VK API.
- Telegram API.

## Env/settings

- `RabbitMq__*`
- `BotManager__Address`, `BotManager__AccessToken`
- `Vk__ApiVersion`, `Vk__WaitSeconds`
- `Telegram__TimeoutSeconds`, `Telegram__RetryDelaySeconds`

## Health/ready/metrics

Это worker service. Проверка выполняется через logs, queue consumption и metrics на `MetricsPort`.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```

Для отладки выберите `debug.bot.integration.yml`.
