# Mazy Platform

Mazy Platform - monorepo для платформы визуального проектирования сценариев и подключения ботов к опубликованным версиям сценариев. Репозиторий не является монолитом: frontend, gateway, backend-сервисы, библиотеки, инфраструктура и tooling живут рядом, но каждый сервис сохраняет свою область ответственности.

## Структура

- `apps` - пользовательские приложения, сейчас `web-client` на Vue/Vite.
- `edge` - edge-слой, сейчас `gateway`, который публикует HTTP JSON API и проксирует gRPC в backend.
- `services` - независимые backend-сервисы по доменам `user`, `scenario`, `bot`, `notification`.
- `libraries` - общие NuGet-библиотеки: контракты, scenario runtime и shared kernel.
- `infra` - docker compose, monitoring и production-инфраструктура.
- `tools` - скрипты для локального запуска и управления пакетами.

## Быстрый локальный запуск

Требуются Docker, Docker Compose, PowerShell, .NET SDK для backend и Node.js/npm для `apps/web-client`.

```powershell
.\tools\start.cmd
```

Скрипт использует `infra/dev/docker-compose.yml` и `infra/dev/.env`, собирает образы и поднимает локальное окружение. При выборе `debug.web-client.yml` frontend запускается локально через `npm run dev`.

Основные URL:

- frontend: `http://localhost:5173`
- gateway: `http://localhost:8080`
- Grafana: `http://localhost:3000`
- RabbitMQ Management: `http://localhost:15672`
- Prometheus и Loki: через Grafana; внутри compose network доступны как `prometheus:9090` и `loki:3100`.

## Документация

- [Обзор платформы](docs/overview.md)
- [Структура репозитория](docs/repository-structure.md)
- [Локальная разработка](docs/local-development.md)
- [Docker Compose окружение](docs/docker-compose.md)
- [Observability](docs/observability.md)
- [Архитектура сервисов](docs/architecture/services.md)
- [Архитектура библиотек](docs/architecture/libraries.md)
- [End-to-end flow платформы](docs/flows/README.md)
- [Конфигурация и secrets](docs/configuration/README.md)
- [Release checklist](docs/release-checklist.md)

## Локальная документация компонентов

- [web-client](apps/web-client/README.md)
- [gateway](edge/gateway/README.md)
- [user-authentication](services/user/authentication/README.md)
- [scenario-repository](services/scenario/repository/README.md)
- [scenario-engine](services/scenario/engine/README.md)
- [bot-manager](services/bot/manager/README.md)
- [bot-integration](services/bot/integration/README.md)
- [notification](services/notification/README.md)
- [contracts](libraries/contracts/README.md)
- [scenario](libraries/scenario/README.md)
- [sharedkernel](libraries/sharedkernel/README.md)