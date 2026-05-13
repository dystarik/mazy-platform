# Docker Compose окружение

Локальный stack лежит в `infra/dev`. Основной файл - `docker-compose.yml`, значения окружения - `infra/dev/.env`. В документацию попадают только имена настроек; реальные значения из `.env` не коммитятся.

## Как запускается

```powershell
.\tools\start.cmd
```

Скрипт переходит в `infra/dev`, собирает образы и выполняет `docker compose --env-file .env up -d`. Debug overlays добавляются как дополнительные `-f debug.*.yml`.

## Инфраструктура

- `postgres` - PostgreSQL для `authentication`, `scenario-repository`, `bot-manager`.
- `rabbitmq` - брокер integration events, management UI на `15672`.
- `mongodb` - scenario graph/runtime documents.
- `loki` - хранение логов.
- `prometheus` - сбор метрик сервисов, workers, gateway, контейнеров и blackbox probes.
- `blackbox-exporter` - HTTP probes для release dashboards.
- `docker-stats-exporter` - метрики Docker-контейнеров.
- `grafana` - dashboards, Explore, datasources Prometheus/Loki.

## Сервисы

- `authentication-migration`, `scenario-repository-migration`, `bot-manager-migration` - одноразовые миграции PostgreSQL.
- `authentication` - user-authentication API.
- `scenario-repository` - scenario repository API.
- `bot-manager` - bot manager API.
- `scenario-engine` - worker исполнения сценариев.
- `bot-integration` - worker интеграций VK/Telegram.
- `notification` - worker email-уведомлений.
- `gateway` - внешний API.
- `web-client` - production build frontend под nginx.

## Порты наружу

- `5173` - web-client (`WEB_CLIENT_PORT`, если задан).
- `8080` - gateway.
- `3000` - Grafana.
- `15672` - RabbitMQ Management.
- `5672` - RabbitMQ AMQP.
- `5432` - PostgreSQL.
- `27017` - MongoDB.

Prometheus (`prometheus:9090`), Loki (`loki:3100`), blackbox exporter и service metrics endpoints остаются внутри `mazy-network`. Смотрите их через Grafana.

## Debug overlays

Overlay убирает выбранный компонент из container runtime или меняет его wiring так, чтобы компонент можно было запустить локально.

- `debug.web-client.yml` - frontend стартует через `npm run dev`.
- `debug.user.authentication.yml`
- `debug.scenario.repository.yml`
- `debug.scenario.engine.yml`
- `debug.bot.integration.yml`

Выбирайте overlay в prompt `tools/dev/start.ps1`. Можно указать несколько номеров через пробел.

## Health и metrics

- Gateway публикует `GET /health/live`, `GET /health/ready`, `GET /metrics` на `localhost:8080`.
- API-сервисы публикуют `health/live` и `health/ready` внутри compose network.
- Workers поднимают diagnostics endpoint на `MetricsPort`.
- Prometheus config: `infra/dev/monitoring/prometheus.yml`.
- Grafana provisioning: `infra/dev/monitoring/grafana/provisioning`.

## Логи

```powershell
cd infra\dev
docker compose --env-file .env logs -f gateway
docker compose --env-file .env logs -f scenario-engine
```

Весь stack:

```powershell
docker compose --env-file .env logs -f
```

Для трассировки запроса возьмите `x-trace-id` из HTTP response gateway и ищите его в logs gateway, backend service и RabbitMQ consumer.

## RabbitMQ

Основной exchange - `mazy.exchange`, тип `topic`. Routing keys живут в contracts и service appsettings. Ошибки уходят в DLX/DLQ, если очередь сервиса так настроена.

После старта проверьте в RabbitMQ Management:

- exchange `mazy.exchange`;
- очереди сервисов;
- bindings на ожидаемые routing keys;
- DLQ, если consumer пишет ошибки.

Ключевые routing keys описаны в [architecture/services.md](architecture/services.md) и flow docs.

## Volumes

Compose создает volumes для PostgreSQL, RabbitMQ, MongoDB, Loki, Prometheus, Grafana и Data Protection keys gateway. Если нужно полностью пересобрать состояние, остановите stack и удалите соответствующие volumes осознанно: это сотрет локальные данные.
