# Наблюдаемость

В dev-окружении есть четыре рабочих входа: container logs, Loki, Prometheus/Grafana и health endpoints. Для связывания HTTP, gRPC и RabbitMQ используется `x-trace-id`.

## Логи

Сервисы пишут структурированные логи через Serilog в console и Loki (`Serilog.Sinks.Grafana.Loki`). В Loki у каждого сервиса есть label `app`: `gateway`, `authentication`, `scenario-repository`, `scenario-engine`, `bot-manager`, `bot-integration`, `notification`.

Логи контейнера:

```powershell
docker compose -f infra/dev/docker-compose.yml --env-file infra/dev/.env logs -f gateway
```

Loki хранит логи dev stack. Конфигурация: `infra/dev/monitoring/loki-config.yaml`. Datasource создается через Grafana provisioning.

Примеры LogQL в Grafana Explore:

```logql
{app="gateway"}
{app="scenario-engine"} |= "TraceId"
{app=~"gateway|authentication|scenario-repository"} |= "it-"
```

Если нужного лога нет в Loki, сначала проверьте container logs. Это быстрее показывает, пишет ли сервис вообще.

## Метрики

Prometheus читает `infra/dev/monitoring/prometheus.yml` и собирает:

- `gateway:8080/metrics`;
- service/workers metrics на `:9090`;
- RabbitMQ metrics на `rabbitmq:15692`;
- container metrics через `docker-stats-exporter:9487`;
- blackbox probes для `/health/live`, `/health/ready` и gRPC TCP endpoints.

Grafana доступна на `http://localhost:3000`. В dev compose включен anonymous admin-доступ; в production такое поведение нужно отключать.

Дашборды Grafana лежат в `infra/dev/monitoring/grafana/provisioning/release-dashboards`:

- `release-overview.json`
- `service-health.json`
- `gateway.json`
- `grpc.json`
- `messaging.json`
- `infrastructure.json`
- `logs-triage.json`

## Health и readiness

- `/health/live` - процесс поднят и отвечает.
- `/health/ready` - сервис готов работать с критичными зависимостями.
- `/metrics` - endpoint для Prometheus.

Gateway публикует эти endpoints наружу на `http://localhost:8080`. API-сервисы и workers доступны внутри compose network; Prometheus собирает их через service name и `MetricsPort`.

Быстрая проверка gateway:

```powershell
Invoke-WebRequest http://localhost:8080/health/live
Invoke-WebRequest http://localhost:8080/health/ready
Invoke-WebRequest http://localhost:8080/metrics
```

Перед release проверьте, что `/health/ready` возвращает unhealthy/503, если отсутствуют production secrets, недоступны БД/RabbitMQ или неверно заданы адреса внутренних сервисов.

## `x-trace-id`

`x-trace-id` связывает один request/event path:

1. frontend может передать `x-trace-id` в HTTP request;
2. gateway принимает его или создает новый;
3. gateway возвращает trace id в response header и gRPC trailers;
4. gateway прокидывает trace id в backend gRPC metadata;
5. RabbitMQ publishers кладут trace id в message headers;
6. consumers читают trace id и добавляют его в logging scope.

Если trace id не пришел, gateway или worker создает новый. В коде ищите `TraceContext.HeaderName`, `TraceIdInterceptor`, `RabbitMqConsumerService` и `RabbitMqIntegrationEventPublisher`.

## Как расследовать запрос

1. Возьмите `x-trace-id` из HTTP response gateway.
2. Откройте Grafana -> Explore -> Loki.
3. Начните с `{app="gateway"} |= "<trace-id>"`.
4. Если запрос ушел в backend, проверьте соответствующий сервис по label `app`.
5. Если был RabbitMQ event, ищите тот же trace id в publisher и consumer logs.
6. Для runtime flows дополнительно смотрите RabbitMQ queue/DLQ и dashboard `messaging`.

## Что обычно ломается

- Readiness красный, live зеленый: сервис жив, но не видит зависимость или не прошел production config check.
- В Grafana нет метрик сервиса: проверьте `MetricsPort`, `prometheus.yml` target и сеть `mazy-network`.
- В Loki нет логов сервиса: проверьте `GrafanaLoki` sink в `appsettings.json` и доступность `loki:3100`.
- Trace id не прошел через событие: проверьте publisher headers и consumer `GetOrCreateTraceId`.
