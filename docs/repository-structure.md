# Структура репозитория

Mazy Platform - monorepo без объединения сервисов в один deployable. Общие контракты и библиотеки лежат рядом с приложениями и сервисами, но каждый сервис имеет собственный проект, Dockerfile, настройки и границы ответственности.

## `apps`

Пользовательские приложения. Сейчас основное приложение - `apps/web-client`: Vue 3 + Vite frontend, который работает с gateway через `/api/v1/*`.

## `edge`

Edge-слой. `edge/gateway` публикует внешний HTTP JSON API, Swagger, health checks и metrics. Внутри gateway проксирует вызовы в backend-сервисы по gRPC и отвечает за JWT validation, CORS, rate limiting и propagation служебных headers.

## `services`

Backend-сервисы по доменным областям:

- `services/user/authentication` - аккаунты, login, registration, sessions, password, MFA, external providers.
- `services/scenario/repository` - проекты, схемы данных, draft/release/history сценариев.
- `services/scenario/engine` - исполнение опубликованных сценариев по входящим событиям от ботов.
- `services/bot/manager` - боты, credentials, project binding, activation, scenario version selection.
- `services/bot/integration` - VK/Telegram polling и публикация входящих bot events.
- `services/notification` - email-уведомления по integration events.

## `libraries`

Общие библиотеки:

- `libraries/contracts` - gRPC/protobuf контракты и integration events.
- `libraries/scenario` - runtime, descriptors, storage и platform-specific части сценариев.
- `libraries/sharedkernel` - общие API/application/domain/infrastructure примитивы.

## `infra`

Инфраструктура:

- `infra/dev` - docker compose для локальной разработки, `.env`, debug overlays, monitoring.
- `infra/prod` - место для production-инфраструктуры и deployment manifests.

## `tools`

Скрипты:

- `tools/start.cmd` / `tools/dev/start.ps1` - локальный запуск compose окружения.
- `tools/cleanup.cmd` / `tools/dev/cleanup.ps1` - очистка dev окружения.
- `tools/packages/*` - публикация и синхронизация версий NuGet-пакетов.
