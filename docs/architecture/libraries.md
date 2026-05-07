# Библиотеки

## `contracts`

Назначение: общий контрактный слой для gRPC API, HTTP JSON transcoding и RabbitMQ integration events.

Содержит:

- `MazyPlatform.Contracts.User.*` - authentication, registration, sessions, password, MFA, linked providers.
- `MazyPlatform.Contracts.Scenario.*` - projects, entity schemas, scenario graph, scenario repository internal API, user data.
- `MazyPlatform.Contracts.Bot.*` - bot manager public/internal API и bot integration events.
- `MazyPlatform.Contracts.Core` - базовые интерфейсы integration events, dispatcher, handler и атрибут routing key.

Используют: gateway, user-authentication, scenario-repository, scenario-engine, bot-manager, bot-integration, notification и web-client через сгенерированные API/types.

Что нельзя ломать без учета зависимых сервисов:

- имена gRPC services/rpc;
- HTTP annotations `/api/v1/*`;
- protobuf field numbers;
- routing keys в `IntegrationEventType`;
- JSON shape integration events.

## `scenario`

Назначение: shared runtime и модель сценариев. Библиотека описывает node descriptors, execution abstractions, storage и platform-specific адаптеры.

Содержит:

- `MazyPlatform.Scenario.Abstractions` - общие interfaces и contracts runtime.
- `MazyPlatform.Scenario` - core descriptors/validation/execution primitives.
- `MazyPlatform.Scenario.Storage.Mongo` - MongoDB storage для runtime данных.
- `MazyPlatform.Scenario.Telegram` и `MazyPlatform.Scenario.Vk` - platform-specific nodes/actions.
- tests для поведения scenario library.

Используют: scenario-repository для validation/catalog, scenario-engine для execution, frontend опирается на совместимые node definitions через API.

Что важно при изменениях:

- изменение node type или параметров влияет на draft graphs и released versions;
- validation rules должны быть совместимы с editor UX;
- runtime contract должен учитывать уже опубликованные версии сценариев.

## `sharedkernel`

Назначение: общие primitives для сервисов.

Содержит:

- `MazyPlatform.SharedKernel.Api` - API/gateway helpers, trace context и common headers.
- `MazyPlatform.SharedKernel.Application` - application abstractions.
- `MazyPlatform.SharedKernel.Domain` - domain primitives.
- `MazyPlatform.SharedKernel.Infrastructure` - infrastructure helpers.

Используют: backend-сервисы и gateway.

Что важно при изменениях:

- `TraceContext.HeaderName` влияет на propagation `x-trace-id`;
- shared exceptions/results должны оставаться совместимыми с API error handling;
- infrastructure helpers не должны тащить сервисные зависимости в domain/application слои.
