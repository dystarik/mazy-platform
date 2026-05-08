# Валидация черновика сценария

## Назначение

Проверить текущий `DraftJson` проекта через `IPlatformScenarioValidator` без изменения состояния сценария.

## Источники истины

- `scenario_repository.proto`: `POST /api/v1/projects/{project_id}/scenario/draft/validate`.
- `ScenarioGraphsServiceProxy.ValidateScenarioDraft`: endpoint требует `[Authorize]`.
- `ScenarioGraphsGrpcService.ValidateScenarioDraft`: берет `OwnerAccountId` из gRPC context.
- `ValidateScenarioDraftHandler`: project access, graph lookup, platform validator.
- `UnitOfWork.SaveChangesAsync` не вызывается: это read/validate flow без изменения `ScenarioGraph` и без domain events.

## Предусловия

- Пользователь авторизован.
- Проект принадлежит пользователю.
- Для проекта создан `ScenarioGraph`.

## Участники

web-client, gateway, scenario-repository, PostgreSQL, `libraries/scenario`.

## Диаграмма

```mermaid
sequenceDiagram
    autonumber
    participant FE as web-client
    participant GW as gateway
    participant SR as scenario-repository
    participant PG as PostgreSQL
    participant LIB as libraries/scenario

    FE->>GW: HTTP POST /api/v1/projects/{project_id}/scenario/draft/validate + Authorization
    GW->>GW: JWT validation
    GW->>SR: gRPC ScenarioGraphService.ValidateScenarioDraft
    Note over GW,SR: metadata: user id, refresh token id, x-trace-id
    SR->>PG: ProjectRepository.GetById(projectId)
    SR->>SR: Проверить OwnerAccountId
    SR->>PG: ScenarioGraphRepository.GetByProjectId(projectId)
    SR->>LIB: IPlatformScenarioValidator.Validate(DraftJson, platformKey)
    Note over SR,LIB: Только чтение draft и platform validation; save/domain events отсутствуют
    alt validator выбросил exception
        SR-->>GW: Internal error ValidationFailed
        GW-->>FE: 5xx/ошибка
    else validator вернул результат
        SR-->>GW: ValidateScenarioDraftResponse { isValid, errors[] }
        GW-->>FE: 200 OK
    end
```

## Транспорты

- HTTP: `POST /api/v1/projects/{project_id}/scenario/draft/validate`.
- gRPC: `ScenarioGraphService.ValidateScenarioDraft`.
- RabbitMQ: не используется, потому что `ValidateScenarioDraftHandler` не изменяет агрегаты и не вызывает `UnitOfWork.SaveChangesAsync`.
- MongoDB: не используется в validate draft; draft читается через `ScenarioGraphRepository`.

## `x-trace-id`

Trace id проходит frontend -> gateway -> scenario-repository. RabbitMQ headers отсутствуют, потому что событий нет.

## Возможные ошибки

- нет JWT или пользователь не авторизован;
- проект не найден;
- проект принадлежит другому пользователю;
- graph не найден;
- validator упал на некорректном JSON/unknown node;
- PostgreSQL недоступен.

## Локальная проверка

1. Создать проект.
2. Сохранить draft через editor.
3. Выполнить validate.
4. Проверить `ValidateScenarioDraftResponse.errors`.
5. Найти `x-trace-id` в logs gateway/scenario-repository.
