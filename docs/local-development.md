# Локальная разработка

## Что нужно установить

- Docker и Docker Compose.
- PowerShell.
- .NET SDK, совместимый с backend-проектами.
- Node.js/npm для `apps/web-client`.
- Свободные порты: `5173`, `8080`, `3000`, `15672`, `5432`, `5672`, `27017`.

## Запуск всего окружения

Из корня репозитория:

```powershell
.\tools\start.cmd
```

`tools/dev/start.ps1` делает три вещи:

1. читает `infra/dev/.env`;
2. собирает Docker-образы из `infra/dev/docker-compose.yml`;
3. запускает compose в фоне.

Если оставить prompt пустым, стартуют все контейнеры. Если выбрать debug overlay, выбранный компонент не поднимается в контейнере и его можно запускать локально.

## Debug overlays

Файлы лежат в `infra/dev`:

- `debug.user.authentication.yml`
- `debug.scenario.repository.yml`
- `debug.scenario.engine.yml`
- `debug.bot.integration.yml`
- `debug.web-client.yml`

Пример: выберите `debug.scenario.engine.yml`, дождитесь старта инфраструктуры и соседних сервисов, затем запускайте `scenario-engine` из IDE или через `dotnet run` с локальными настройками.

## Frontend локально

```powershell
cd apps\web-client
npm install
npm run dev
```

Vite proxy отправляет `/api` на `http://localhost:8080`. Для обычного dev-режима `VITE_API_URL` можно оставить пустым. `VITE_*` значения попадают в client bundle, поэтому secrets там быть не должно.

## Полезные URL

- `http://localhost:5173` - frontend.
- `http://localhost:8080` - gateway API, Swagger, health, metrics.
- `http://localhost:3000` - Grafana.
- `http://localhost:15672` - RabbitMQ Management.
- `localhost:5432` - PostgreSQL.
- `localhost:27017` - MongoDB.

Prometheus (`prometheus:9090`) и Loki (`loki:3100`) доступны внутри compose network и уже подключены к Grafana.

## Проверка после запуска

```powershell
Invoke-WebRequest http://localhost:8080/health/live
Invoke-WebRequest http://localhost:8080/health/ready
Invoke-WebRequest http://localhost:8080/metrics
```

Затем проверьте руками:

1. frontend открывается на `http://localhost:5173`;
2. Grafana видит Prometheus и Loki datasources;
3. RabbitMQ Management показывает exchange `mazy.exchange`;
4. после старта consumers появились очереди сервисов;
5. smoke flow проходит до сохранения draft и `validate draft`.

## Логи и диагностика

Логи конкретного контейнера:

```powershell
docker compose -f infra/dev/docker-compose.yml --env-file infra/dev/.env logs -f gateway
```

Логи всего окружения:

```powershell
docker compose -f infra/dev/docker-compose.yml --env-file infra/dev/.env logs -f
```

Смотрите `x-trace-id` в HTTP response gateway, затем ищите этот же trace id в Grafana Explore или container logs.

## Частые сбои

- `.env` не найден: проверьте `infra/dev/.env`. Скрипт без него не стартует.
- Порт занят: остановите старый процесс/compose stack или поменяйте соответствующую настройку в `.env`, если она вынесена туда.
- `gateway /health/ready` падает: смотрите логи `gateway` и readiness сервисов, от которых он зависит.
- Миграция завершилась с ошибкой: откройте logs контейнера `*-migration`, затем проверьте PostgreSQL credentials и доступность `postgres`.
- Frontend не ходит в API: проверьте, что gateway доступен на `8080`, а Vite proxy видит `/api`.
- Нет событий в RabbitMQ: проверьте consumers, exchange `mazy.exchange`, routing keys и DLQ.
- Нет логов/метрик в Grafana: проверьте `prometheus`, `loki`, `docker-stats-exporter`, `blackbox-exporter` и provisioning в `infra/dev/monitoring`.

## Очистка локальных артефактов

```powershell
.\tools\cleanup.cmd
```

Перед удалением можно посмотреть, что будет затронуто:

```powershell
.\tools\dev\cleanup.ps1 -DryRun
```

Скрипт удаляет найденные `bin`, `obj`, `node_modules`, `.nuxt`, `.output`, `dist`, `.cache`, кроме вложенных `node_modules` внутри `node_modules`.
