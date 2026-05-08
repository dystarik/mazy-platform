# Удаление версии сценария

## Назначение

Удалить опубликованную версию сценария, если на нее не ссылаются боты. Если удаляется текущий release, domain выбирает максимальную оставшуюся версию как новый release или сбрасывает release, если версий больше нет.

## Источники истины

- `scenario_repository.proto`: `DELETE /api/v1/projects/{project_id}/scenario/versions/{version}`.
- `DeleteScenarioVersionHandler`: project access, internal bot check, `graph.DeleteVersion`.
- `BotInternalService.HasBotsOnScenarioVersion`: internal bot-manager query.
- `ScenarioGraph.DeleteVersion`: domain events `ScenarioVersionDeleted`, optional `ScenarioReleaseChanged` или `ScenarioReleaseRemoved`.
- `UnitOfWork.SaveChangesAsync`: сохраняет удаление версии, затем dispatch'ит все domain events, которые `ScenarioGraph.DeleteVersion` добавил в агрегат.

## Предусловия

- Пользователь авторизован.
- Проект принадлежит пользователю.
- Версия существует.
- Нет bot instances с `ProjectId` и `ScenarioVersion`, равными удаляемой версии.

## Участники

web-client, gateway, scenario-repository, bot-manager, PostgreSQL, RabbitMQ, bot-integration, scenario-engine.

## Диаграмма

```mermaid
sequenceDiagram
    autonumber
    participant FE as web-client
    participant GW as gateway
    participant SR as scenario-repository
    participant BM as bot-manager
    participant SRPG as PostgreSQL scenario
    participant BMPG as PostgreSQL bot
    participant MQ as RabbitMQ mazy.exchange
    participant BI as bot-integration
    participant SE as scenario-engine

    FE->>GW: HTTP DELETE /api/v1/projects/{project_id}/scenario/versions/{version}
    GW->>SR: gRPC ScenarioGraphService.DeleteScenarioVersion
    SR->>SRPG: Get project, check owner
    SR->>SRPG: Get ScenarioGraph
    SR->>BM: internal gRPC BotInternalService.HasBotsOnScenarioVersion
    Note over SR,BM: metadata: x-internal-token, x-trace-id
    BM->>BMPG: Any BotInstance(projectId, scenarioVersion)
    alt Есть боты на версии
        BM-->>SR: hasBots=true
        SR-->>GW: Conflict VersionInUse
        GW-->>FE: 409/ошибка
    else Ботов на версии нет
        BM-->>SR: hasBots=false
        SR->>SRPG: graph.DeleteVersion(version)
        SR->>SRPG: UnitOfWork.SaveChangesAsync: persist version removal/current release
        SR->>SR: Dispatch ScenarioVersionDeletedDomainEvent
        SR--)MQ: scenario.repository.scenario-version-deleted
        opt Удален текущий release и остались версии
            SR->>SR: Dispatch ScenarioReleaseChangedDomainEvent
            SR--)MQ: scenario.repository.scenario-release-changed
            MQ--)BM: queue bot-manager.scenario.release-changed
            BM->>BMPG: ApplyScenarioRelease для auto-update ботов
            BM->>BMPG: UnitOfWork.SaveChangesAsync
            BM->>BM: Dispatch BotInstanceScenarioVersionChangedDomainEvent
            BM--)MQ: bot.manager.bot-instance-scenario-version-changed
            MQ--)BI: update version cache
            MQ--)SE: update version cache
        end
        opt Удален последний release
            SR->>SR: Dispatch ScenarioReleaseRemovedDomainEvent
            SR--)MQ: scenario.repository.scenario-release-removed
            MQ--)BM: queue bot-manager.scenario.release-removed
            BM->>BMPG: Deactivate active bots проекта
            BM->>BMPG: UnitOfWork.SaveChangesAsync
            BM->>BM: Dispatch BotInstanceDeactivatedDomainEvent
            BM--)MQ: bot.manager.bot-instance-deactivated
            MQ--)BI: stop poller/remove cache
            MQ--)SE: remove cache
        end
        GW-->>FE: 200 OK
    end
```

## Транспорты

- HTTP: `DELETE /api/v1/projects/{project_id}/scenario/versions/{version}`.
- gRPC: public `ScenarioGraphService.DeleteScenarioVersion`, internal `BotInternalService.HasBotsOnScenarioVersion`.
- RabbitMQ: `ScenarioVersionDeletedDomainEvent` -> `scenario.repository.scenario-version-deleted`; если удален текущий release, дополнительно `ScenarioReleaseChangedDomainEvent` -> `scenario.repository.scenario-release-changed` или `ScenarioReleaseRemovedDomainEvent` -> `scenario.repository.scenario-release-removed`.

## `x-trace-id`

Gateway прокидывает trace id в scenario-repository. Scenario-repository добавляет его в internal gRPC metadata и RabbitMQ headers.

## Возможные ошибки

- версия не найдена;
- проект не найден или нет доступа;
- версия используется ботами;
- internal token к bot-manager неверен;
- PostgreSQL/RabbitMQ недоступны;
- downstream consumers обработают событие с ошибкой и сообщение уйдет в DLQ.

## Локальная проверка

1. Опубликовать две версии.
2. Проверить, что удаляемая версия не выбрана ботом.
3. Удалить версию.
4. Проверить history и RabbitMQ events.
5. Если удаляли текущую версию, проверить cache update у bot-integration/scenario-engine.
