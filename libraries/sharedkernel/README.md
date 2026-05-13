# sharedkernel

Shared kernel содержит общие building blocks backend-сервисов: API helpers, application/domain primitives и infrastructure utilities. Это не место для логики конкретного сервиса.

## Где смотреть код

- `src/MazyPlatform.SharedKernel.Api` - API helpers, trace context, headers/interceptors.
- `src/MazyPlatform.SharedKernel.Application` - application-level abstractions.
- `src/MazyPlatform.SharedKernel.Domain` - domain primitives.
- `src/MazyPlatform.SharedKernel.Infrastructure` - shared infrastructure helpers.

Используют gateway, backend-сервисы в `services/*` и библиотеки, которым нужны общие domain/application abstractions.

## Как проверять

```powershell
dotnet build libraries\sharedkernel\MazyPlatform.SharedKernel.slnx
```

Если менялись public API, дополнительно собирайте потребителей, которые затронуты:

- `edge/gateway` для API/trace/header изменений;
- сервисы в `services/*` для application/domain primitives;
- библиотеки `libraries/contracts` и `libraries/scenario`, если они ссылаются на измененные abstractions.

## Что держать в голове

- `x-trace-id` propagation зависит от shared trace primitives.
- Domain/application primitives должны оставаться независимыми от PostgreSQL, RabbitMQ, HTTP и конкретных сервисов.
- Infrastructure helpers не должны тянуть в Domain/Application runtime-зависимости.
- Общие types лучше добавлять только после второго реального потребителя.

## Рискованные изменения

- Breaking public API ломает несколько сервисов сразу, даже если sharedkernel собирается.
- Изменения trace/header behavior ухудшают диагностику gateway -> service -> worker цепочек.
- Слишком конкретная абстракция быстро превращает sharedkernel в скрытую зависимость между bounded contexts.
- Новые dependencies в sharedkernel увеличивают transitive dependency graph всех сервисов.
