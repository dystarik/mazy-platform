# Локальная разработка

## Зависимости

- Docker и Docker Compose.
- PowerShell.
- .NET SDK, совместимый с проектами backend.
- Node.js/npm для `apps/web-client`.
- Доступ к локальным портам `5173`, `8080`, `3000`, `9090`, `15672`, `5432`, `5672`, `27017`.

## Быстрый запуск

Из корня репозитория:

```powershell
.\tools\start.cmd
```

Скрипт вызывает `tools/dev/start.ps1`, читает `infra/dev/.env`, собирает образы и запускает `infra/dev/docker-compose.yml`. Если выбрать debug overlay, соответствующий сервис можно запускать локально из IDE, а остальные зависимости останутся в compose.

## Debug overlays

В `infra/dev` есть overlays:

- `debug.user.authentication.yml`
- `debug.scenario.repository.yml`
- `debug.scenario.engine.yml`
- `debug.bot.integration.yml`
- `debug.web-client.yml`

При запуске `tools/dev/start.ps1` скрипт предлагает выбрать один или несколько overlays по номеру.

## Frontend локально

```powershell
cd apps\web-client
npm install
npm run dev
```

Vite proxy отправляет `/api` на `http://localhost:8080`. Переменная `VITE_API_URL` может быть пустой, если frontend работает через dev proxy или nginx proxy в контейнере.

## Основные localhost URL

- `http://localhost:5173` - frontend.
- `http://localhost:8080` - gateway API, Swagger, health, metrics.
- `http://localhost:3000` - Grafana.
- Prometheus - через Grafana datasource; внутри compose network `http://prometheus:9090`.
- Loki - через Grafana Explore; внутри compose network `http://loki:3100`.
- `http://localhost:15672` - RabbitMQ Management.
- `localhost:5432` - PostgreSQL.
- `localhost:27017` - MongoDB.

## Логи, metrics и health

- Логи контейнеров: `docker compose -f infra/dev/docker-compose.yml logs -f <service>`.
- Health gateway: `http://localhost:8080/health/live` и `http://localhost:8080/health/ready`.
- Metrics gateway: `http://localhost:8080/metrics`.
- Metrics сервисов внутри compose собирает Prometheus; если сервис не публикует порт наружу, смотрите через Prometheus/Grafana.
- RabbitMQ queues и exchanges смотрите в management UI.

## Проверка после запуска

1. Открыть frontend.
2. Проверить `gateway /health/live` и `/health/ready`.
3. Проверить, что Grafana показывает datasource Prometheus и Loki.
4. Проверить в RabbitMQ наличие exchange `mazy.exchange` и очередей сервисов после старта consumers.
5. Выполнить smoke flow: регистрация/логин, создание проекта, сохранение draft, validate draft.
