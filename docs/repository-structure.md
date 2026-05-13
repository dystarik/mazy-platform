# Структура репозитория

Репозиторий держит приложения, сервисы, общие библиотеки, инфраструктуру и локальные скрипты рядом. Сервисы собираются и запускаются отдельно; общие контракты и runtime-код лежат в `libraries`.

## `apps`

- `apps/web-client` - Vue 3 + Vite frontend. Работает с backend через gateway и `/api/v1/*`.
- Локальный README: [../apps/web-client/README.md](../apps/web-client/README.md).

## `edge`

- `edge/gateway` - публичная backend-точка. Публикует HTTP JSON API, Swagger, health checks и metrics.
- Gateway вызывает backend-сервисы по gRPC и прокидывает служебные headers: `x-trace-id`, user id, refresh token id, client IP.
- Локальный README: [../edge/gateway/README.md](../edge/gateway/README.md).

## `services`

Backend разбит по доменам:

- `services/user/authentication` - аккаунты, registration/login, sessions, password, MFA, external providers.
- `services/scenario/repository` - проекты, entity schemas, draft/release/history сценариев, user data records.
- `services/scenario/engine` - worker исполнения опубликованных сценариев.
- `services/bot/manager` - bot instances, encrypted credentials, binding к проекту, activation, scenario version selection.
- `services/bot/integration` - workers для VK/Telegram polling и публикации входящих bot events.
- `services/notification` - email-уведомления по RabbitMQ events.

Каждый сервис хранит свой README в собственной директории. Начинайте с него, если меняете конкретный компонент.

## `libraries`

- `libraries/contracts` - protobuf/gRPC contracts, HTTP annotations и RabbitMQ integration events.
- `libraries/scenario` - descriptors, validation, execution runtime, Mongo storage и platform adapters.
- `libraries/sharedkernel` - общие API/application/domain/infrastructure primitives.

Изменения в `libraries` обычно затрагивают несколько сервисов. Перед правкой проверьте раздел [architecture/libraries.md](architecture/libraries.md).

## `infra`

- `infra/dev` - Docker Compose для локальной разработки, `.env`, debug overlays и monitoring.
- `infra/dev/monitoring` - Loki, Prometheus, Grafana provisioning, blackbox exporter и docker stats exporter.
- `infra/prod` - место для production manifests, если они появятся.

## `tools`

- `tools/start.cmd` / `tools/dev/start.ps1` - сборка и запуск dev compose.
- `tools/cleanup.cmd` / `tools/dev/cleanup.ps1` - удаление `bin`, `obj`, `node_modules`, `dist` и других локальных артефактов.
- `tools/packages/*` - публикация и синхронизация версий NuGet-пакетов.

## Где искать код

- HTTP paths: `libraries/contracts/**/**/*.proto`, затем proxy в `edge/gateway/src`.
- gRPC implementation: `services/**/src/*Api` или worker `Program.cs`.
- RabbitMQ routing keys: `libraries/contracts/**/Events` и service `Messaging/RabbitMq*`.
- Scenario nodes/runtime: `libraries/scenario/src`.
- Monitoring config: `infra/dev/monitoring`.
