# Входящее сообщение от бота

## Назначение

Получить update из VK/Telegram, нормализовать его в `BotIncomingEventIntegrationEvent`, передать в scenario-engine через RabbitMQ, загрузить scenario graph при cache miss, получить/создать session и выполнить сценарий.

## Источники истины

- `VkPoller`, `TelegramPoller`: polling provider API и publish `BotIncomingEventIntegrationEvent`.
- `RabbitMqEventPublisher`: routing key `bot.integration.incoming_event`.
- `BotIncomingEventIntegrationEventHandler`: cache lookup, parse incoming event, load scenario, session, executor.
- `ScenarioLoader`: cache -> `ScenarioGraphService.GetScenarioByVersion`.
- В этом flow нет EF `UnitOfWork` и доменных событий: bot-integration публикует integration event напрямую из poller, scenario-engine сохраняет runtime session через `sessionStore.SaveAsync`.

## Предусловия

- Бот активен.
- bot-integration и scenario-engine уже получили `bot.manager.bot-instance-activated` или initial sync.
- Bot cache содержит `botId -> projectId/scenarioVersion/platform/accessToken`.
- Release version существует в scenario-repository.

## Участники

VK/Telegram, bot-integration, RabbitMQ, scenario-engine, scenario-repository, Mongo/session store, provider API.

## Диаграмма

```mermaid
sequenceDiagram
    autonumber
    participant EXT as VK/Telegram
    participant BI as bot-integration
    participant MQ as RabbitMQ mazy.exchange
    participant SE as scenario-engine
    participant SR as scenario-repository
    participant STORE as Session/runtime store
    participant EXEC as scenario executor
    participant API as VK/Telegram API

    EXT-->>BI: Long polling update
    alt VK
        BI->>BI: VkPoller: accept type=message_new
    else Telegram
        BI->>BI: TelegramPoller: accept message/callback_query, update offset
    end
    BI->>BI: Build BotIncomingEventIntegrationEvent(botId, projectId, scenarioVersion, rawPayload)
    BI--)MQ: bot.integration.incoming_event + x-trace-id
    MQ--)SE: queue scenario-engine.bot-incoming
    SE->>SE: BotIncomingEventIntegrationEventHandler
    SE->>SE: Check supported platform and BotInstanceCache
    alt Bot not cached or platform mismatch
        SE-->>MQ: Ack, лог warning, без исполнения
    else Bot cached
        SE->>SE: ScenarioLoader.GetOrLoad(projectId, version, platformKey)
        alt Scenario not in cache
            SE->>SR: gRPC ScenarioGraphService.GetScenarioByVersion
            SR-->>SE: graphJson
            SE->>SE: Build ScenarioGraph and cache
        end
        SE->>SE: Parse Vk/Telegram incoming event
        SE->>STORE: GetOrCreate session(botId, platformUserId, startNodeId)
        opt Start command
            SE->>STORE: Delete old session, create from start node
        end
        SE->>EXEC: ExecuteAsync(graph, incomingEvent, session, projectId, version, accessToken)
        EXEC->>API: Send provider actions/messages as required by nodes
        SE->>STORE: sessionStore.SaveAsync(session)
        Note over SE,STORE: Runtime state save, без domain event/RabbitMQ publish
        SE-->>MQ: Ack
    end
```

## Транспорты

- Provider API: VK long poll или Telegram getUpdates.
- RabbitMQ: `bot.integration.incoming_event` публикуется напрямую из `VkPoller`/`TelegramPoller`; scenario-engine на успешное исполнение новых RabbitMQ-событий не публикует.
- gRPC: `ScenarioGraphService.GetScenarioByVersion` только если scenario cache miss.
- Provider API: outgoing actions из executor.

## `x-trace-id`

Provider update обычно не содержит trace id, поэтому bot-integration создает новый через `TraceContext.GetOrCreate()` при publish. Scenario-engine читает header из RabbitMQ и использует его при logs и gRPC cache miss.

## Возможные ошибки

- provider polling error/timeout;
- publish в RabbitMQ failed;
- bot отсутствует в cache;
- platform mismatch;
- scenario version не найдена;
- raw payload не парсится;
- session store недоступен;
- provider API отклонил outgoing action.

## Локальная проверка

1. Активировать бота и убедиться, что poller запущен.
2. Отправить сообщение боту.
3. Проверить publish `bot.integration.incoming_event`.
4. Проверить logs scenario-engine по trace id.
5. Проверить outgoing response во внешней платформе.
