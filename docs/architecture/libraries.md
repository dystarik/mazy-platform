# Библиотеки

Общие библиотеки лежат в `libraries/*`. Они не поставляются как отдельные сервисы, но задают контракты между сервисами, frontend и runtime сценариев. Изменение здесь обычно затрагивает несколько компонентов.

## `libraries/contracts`

Контрактный слой для gRPC, HTTP JSON transcoding и RabbitMQ events.

Код:

- `libraries/contracts/src/MazyPlatform.Contracts.*.Grpc` - protobuf/gRPC contracts.
- `libraries/contracts/src/MazyPlatform.Contracts.*` - integration events.
- `libraries/contracts/src/MazyPlatform.Contracts.Core` - `IntegrationEventTypeAttribute`, dispatcher/handler abstractions.

Используют: `edge/gateway`, все backend-сервисы, workers и `apps/web-client` через сгенерированные API/types.

Синхронная граница:

- protobuf service/rpc names задают gRPC contract;
- HTTP annotations в `.proto` задают внешний `/api/v1/*` contract gateway.

Асинхронная граница:

- `[IntegrationEventType("...")]` задает RabbitMQ routing key;
- event class задает JSON payload.

Нельзя менять без миграционного плана:

- protobuf field numbers;
- service/rpc names;
- HTTP annotations;
- routing keys;
- required semantics полей event payload.

Риск: старый consumer может получить новый JSON payload или перестать получать событие из-за смены routing key. Старый frontend может продолжить вызывать прежний `/api/v1/*` endpoint.

## `libraries/scenario`

Runtime и модель сценариев. Эта библиотека связывает editor, repository validation и engine execution.

Код:

- `libraries/scenario/src/MazyPlatform.Scenario.Abstractions` - runtime interfaces и contracts.
- `libraries/scenario/src/MazyPlatform.Scenario` - descriptors, validation, execution primitives.
- `libraries/scenario/src/MazyPlatform.Scenario.Storage.Mongo` - MongoDB storage для runtime данных.
- `libraries/scenario/src/Platforms/MazyPlatform.Scenario.Telegram` - Telegram nodes/actions.
- `libraries/scenario/src/Platforms/MazyPlatform.Scenario.Vk` - VK nodes/actions.
- `libraries/scenario/tests/MazyPlatform.Scenario.Tests` - tests поведения runtime.

Используют:

- `scenario-repository` для catalog/validation draft и release graph;
- `scenario-engine` для execution;
- frontend опирается на совместимые node definitions через API.

Граница данных:

- draft/release graph хранит `scenario-repository`;
- execution state хранит `scenario-engine` через Mongo-backed storage;
- node type и параметры должны читаться как минимум для уже опубликованных versions.

Ломается при изменении:

- переименование node type;
- удаление или смена типа параметра node;
- изменение validation rule без учета editor UX;
- изменение runtime contract без совместимости с released scenario versions.

Риск: опубликованный сценарий может стать неисполняемым, даже если draft validation для новых графов проходит.

## `libraries/sharedkernel`

Набор общих primitives для backend-сервисов и gateway.

Код:

- `libraries/sharedkernel/src/MazyPlatform.SharedKernel.Api` - API helpers, common headers, trace context.
- `libraries/sharedkernel/src/MazyPlatform.SharedKernel.Application` - application abstractions.
- `libraries/sharedkernel/src/MazyPlatform.SharedKernel.Domain` - domain primitives.
- `libraries/sharedkernel/src/MazyPlatform.SharedKernel.Infrastructure` - infrastructure helpers.

Используют: backend-сервисы и gateway.

Граница:

- `TraceContext.HeaderName` влияет на propagation `x-trace-id`;
- common results/exceptions влияют на API error handling;
- infrastructure helpers не должны протаскивать сервисные зависимости в domain/application projects.

Ломается при изменении:

- header names;
- exception/result shape, который мапится в gRPC/HTTP ошибки;
- abstractions, используемых несколькими сервисами.
