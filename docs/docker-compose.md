# Docker Compose окружение

Локальное окружение находится в `infra/dev`. Основной файл - `docker-compose.yml`, настройки берутся из `infra/dev/.env`. Реальные значения из `.env` не должны попадать в документацию или commit history.

## Инфраструктурные контейнеры

- `postgres` - PostgreSQL для user-authentication, scenario-repository и bot-manager.
- `rabbitmq` - брокер integration events, management UI на `15672`.
- `mongodb` - хранение scenario graph/runtime данных.
- `loki` - сбор логов.
- `prometheus` - сбор metrics.
- `docker-stats-exporter` - metrics по контейнерам.
- `grafana` - dashboards и exploration logs/metrics.

## Сервисные контейнеры

- `authentication-migration` - миграции БД user-authentication.
- `authentication` - user-authentication service.
- `scenario-repository-migration` - миграции БД scenario-repository.
- `scenario-repository` - scenario repository service.
- `scenario-engine` - scenario execution worker.
- `bot-manager-migration` - миграции БД bot-manager.
- `bot-manager` - bot manager service.
- `bot-integration` - VK/Telegram integration worker.
- `notification` - email notification worker.
- `gateway` - внешний API gateway.
- `web-client` - production build frontend под nginx.

## Порты

- `5173` - web-client.
- `8080` - gateway.
- `3000` - Grafana.
- `15672` - RabbitMQ Management.
- `5672` - RabbitMQ AMQP.
- `5432` - PostgreSQL.
- `27017` - MongoDB.

Prometheus (`prometheus:9090`) и Loki (`loki:3100`) доступны внутри `mazy-network` и подключены к Grafana. Отдельные localhost ports для них в базовом compose не публикуются.

Внутренние gRPC endpoints сервисов не обязаны публиковаться наружу. В compose они доступны по service name внутри `mazy-network`.

## Логи

```powershell
cd infra\dev
docker compose --env-file .env logs -f gateway
docker compose --env-file .env logs -f scenario-engine
```

Для всего окружения:

```powershell
docker compose --env-file .env logs -f
```

## Health и metrics

- Gateway публикует `GET /health/live`, `GET /health/ready`, `GET /metrics`.
- Backend API-сервисы публикуют `health/live` и `health/ready` на своих HTTP/gRPC endpoints внутри compose.
- Worker-сервисы пишут diagnostics logs и metrics на `MetricsPort`.
- Prometheus конфигурация лежит в `infra/dev/monitoring/prometheus.yml`.
- Grafana provisioning лежит в `infra/dev/monitoring/grafana/provisioning`.

## RabbitMQ

Основной exchange: `mazy.exchange`, тип `topic`. Для ошибок используются DLX/DLQ, определенные в appsettings сервисов. Ключевые routing keys описаны в [architecture/services.md](architecture/services.md) и flow docs.
