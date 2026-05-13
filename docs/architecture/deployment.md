# Развертывание

Mazy Platform - monorepo с несколькими единицами поставки. Сервисы не собираются в один backend-процесс. У каждого backend-сервиса есть свой проект, Dockerfile и runtime-настройки.

## Единицы поставки

- `apps/web-client` - статический Vue/Vite frontend, в dev-compose отдается nginx.
- `edge/gateway` - публичный edge API.
- `services/user/authentication` - user domain.
- `services/scenario/repository` - scenario metadata, draft/release/history.
- `services/scenario/engine` - worker исполнения сценариев.
- `services/bot/manager` - bot metadata, credentials и bindings.
- `services/bot/integration` - worker polling VK/Telegram.
- `services/notification` - worker email-уведомлений.

Инфраструктурные зависимости: PostgreSQL, MongoDB, RabbitMQ, Prometheus, Loki, Grafana, blackbox-exporter, docker-stats-exporter.

## Dev-compose

Локальная схема описана в `infra/dev/docker-compose.yml`.

Публичные порты dev-compose:

- `web-client`: `${WEB_CLIENT_PORT:-5173}:80`
- `gateway`: `8080:8080`
- `postgres`: `5432:5432`
- `rabbitmq`: `5672:5672`, management `15672:15672`
- `mongodb`: `27017:27017`
- `grafana`: `3000:3000`

Сервисы подключены к bridge network `mazy-network`. Gateway вызывает backend по service DNS names:

- `authentication:8081`
- `scenario-repository:6101`
- `bot-manager:5101`

Internal gRPC endpoints используют отдельные host/port bindings внутри сервисов:

- `bot-manager:5102` для `BotInternalService`;
- `scenario-repository:6102` для `ScenarioRepositoryInternalService`.

## Storage boundaries

PostgreSQL используется несколькими доменами, но базы разделены переменными compose:

- `${POSTGRES_DB_AUTH}` для `authentication`;
- `${POSTGRES_DB_SCENARIO}` для `scenario-repository`;
- `${POSTGRES_DB_BOT}` для `bot-manager`.

MongoDB хранит scenario graph/runtime data через `${MONGO_DB_SCENARIO}`. RabbitMQ переносит integration events через topic exchange, настроенный в сервисных `appsettings`.

## Startup boundaries

В dev-compose migration containers запускаются отдельно от сервисов:

- `authentication-migration`
- `scenario-repository-migration`
- `bot-manager-migration`

Основные сервисы зависят от successful migration job и health/start conditions инфраструктуры. Это локальная схема. Production-процесс миграций в этих документах не задан.

## Сетевые и security boundaries

Gateway - единственная публичная backend-точка для frontend. Сервисные публичные gRPC endpoints должны оставаться во внутренней сети. Internal gRPC endpoints (`BotInternalService`, `ScenarioRepositoryInternalService`) дополнительно проверяют internal API tokens:

- `INTERNAL_API_TOKEN_BOT_MANAGER`
- `INTERNAL_API_TOKEN_SCENARIO_REPOSITORY`

Secrets передаются через переменные окружения compose. Для другого окружения нужен эквивалентный способ доставки secret values; конкретный production secret manager здесь не описан.

## Observability boundaries

Gateway и сервисы публикуют `/health/live`, `/health/ready` и metrics. Workers поднимают diagnostics host. Monitoring stack в dev-compose собирает metrics и logs через Prometheus, Loki и Grafana.

`x-trace-id` должен сохраняться на HTTP, gRPC и RabbitMQ границах. Если сервис создает новое событие без входящего trace id, он создает новый trace id и кладет его в headers.

## Что ломается при изменениях

- смена service DNS name, port или env key ломает gateway/workers в dev-compose;
- смена internal token без синхронного обновления caller/callee ломает internal gRPC;
- удаление migration job или изменение порядка startup может поднять сервис на несогласованной схеме БД;
- публикация RabbitMQ management, metrics или service gRPC наружу расширяет attack surface;
- смена PostgreSQL/MongoDB database names без миграции данных приводит к пустому окружению для соответствующего домена.
