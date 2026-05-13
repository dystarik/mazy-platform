# Mazy Platform

Mazy Platform - monorepo для визуального редактора сценариев, backend-сервисов и интеграций с ботами. Пользователь собирает сценарий в `web-client`, публикует release-версию, подключает бота и выбирает, какую версию исполнять.

Внешний вход в backend один: `edge/gateway`. Остальные сервисы общаются через gRPC и RabbitMQ, хранят свои данные в PostgreSQL/MongoDB и поставляются отдельными контейнерами.

## Быстрый старт

Нужны Docker, Docker Compose, PowerShell, .NET SDK и Node.js/npm.

```powershell
.\tools\start.cmd
```

Скрипт читает `infra/dev/.env`, собирает образы из `infra/dev/docker-compose.yml` и запускает окружение. Если при запуске выбрать debug overlay, выбранный компонент можно поднимать локально из IDE, а зависимости останутся в Docker.

После старта проверьте:

- frontend: `http://localhost:5173`
- gateway, Swagger, health, metrics: `http://localhost:8080`
- Grafana: `http://localhost:3000`
- RabbitMQ Management: `http://localhost:15672`

Prometheus и Loki не публикуются отдельными localhost-портами. Открывайте их через Grafana.

## Куда идти дальше

- [docs/README.md](docs/README.md) - общий индекс документации.
- [docs/overview.md](docs/overview.md) - карта продукта, сервисов и основных сценариев.
- [docs/local-development.md](docs/local-development.md) - локальный запуск, debug overlays, проверки и типичные поломки.
- [docs/architecture/README.md](docs/architecture/README.md) - архитектурные границы сервисов, библиотек и deployment.
- [docs/flows/README.md](docs/flows/README.md) - end-to-end flows: auth, scenario, bot, notification.

## Основные директории

- [apps/web-client](apps/web-client/README.md) - Vue 3 + Vite frontend.
- [edge/gateway](edge/gateway/README.md) - публичный HTTP JSON API поверх gRPC contracts.
- [services/user/authentication](services/user/authentication/README.md) - аккаунты, сессии, пароль, MFA, external providers.
- [services/scenario/repository](services/scenario/repository/README.md) - проекты, схемы данных, draft/release версии сценариев.
- [services/scenario/engine](services/scenario/engine/README.md) - исполнение опубликованных сценариев.
- [services/bot/manager](services/bot/manager/README.md) - боты, credentials, привязка к проектам и версиям сценариев.
- [services/bot/integration](services/bot/integration/README.md) - polling VK/Telegram и публикация входящих событий.
- [services/notification](services/notification/README.md) - email-уведомления из RabbitMQ events.
- [libraries/contracts](libraries/contracts/README.md), [libraries/scenario](libraries/scenario/README.md), [libraries/sharedkernel](libraries/sharedkernel/README.md) - общие NuGet-библиотеки.
- [infra/dev](docs/docker-compose.md) - Docker Compose, monitoring, debug overlays.
- [tools](tools) - запуск окружения, очистка, публикация и синхронизация пакетов.
