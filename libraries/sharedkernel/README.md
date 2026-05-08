# sharedkernel

Shared kernel содержит общие building blocks для backend-сервисов: API helpers, application/domain primitives и infrastructure utilities.

## Используют

- `edge/gateway`
- backend-сервисы в `services/*`
- библиотеки, которым нужны общие domain/application abstractions.

## Основные модули

- `MazyPlatform.SharedKernel.Api` - API helpers, trace context, common headers/interceptors.
- `MazyPlatform.SharedKernel.Application` - application-level abstractions.
- `MazyPlatform.SharedKernel.Domain` - domain primitives.
- `MazyPlatform.SharedKernel.Infrastructure` - infrastructure helpers.

## Важно при изменениях

- `x-trace-id` propagation зависит от shared trace primitives.
- Domain/application primitives должны оставаться независимыми от конкретных сервисов.
- Любое изменение public API sharedkernel требует проверки всех зависимых сервисов.
