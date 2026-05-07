# Наблюдаемость

Наблюдаемость в dev-окружении состоит из logs, metrics, health checks, readiness checks и trace correlation через `x-trace-id`.

## Логи

Сервисы используют Serilog и добавляют свойство `Application`. Gateway дополнительно обогащает HTTP request logs полями host, protocol, user agent, remote IP, `TraceId` и ASP.NET trace identifier.

Локально:

```powershell
docker compose -f infra/dev/docker-compose.yml --env-file infra/dev/.env logs -f <service>
```

В Grafana используйте Explore -> Loki и фильтр по `Application` или `TraceId`.

## Loki

Loki хранит logs из dev окружения. Конфигурация лежит в `infra/dev/monitoring/loki-config.yaml`. Grafana datasource создается provisioning-файлами.

## Prometheus

Prometheus собирает `/metrics` у gateway и сервисов, а также metrics контейнеров через `docker-stats-exporter`. Конфигурация лежит в `infra/dev/monitoring/prometheus.yml`.

## Grafana

Grafana доступна на `http://localhost:3000`. В dev compose включен anonymous admin-доступ для локального использования. Для production это поведение нужно переопределять.

## Health checks и readiness

- `/health/live` - процесс жив и может отвечать.
- `/health/ready` - сервис готов обслуживать запросы с учетом критичных зависимостей и production-конфигурации.
- `/metrics` - Prometheus endpoint.

Gateway публикует эти endpoints наружу на `http://localhost:8080`. Остальные сервисы доступны внутри compose network или через debug overlay.

## `x-trace-id`

Header `x-trace-id` должен проходить через весь request/event path:

1. frontend может передать `x-trace-id` в HTTP request;
2. gateway принимает или создает trace id;
3. gateway возвращает trace id в response header и gRPC trailers;
4. gateway прокидывает trace id в backend gRPC calls;
5. RabbitMQ publishers кладут trace id в message headers;
6. consumers читают trace id из message headers и добавляют его в logging scope.

Если header не пришел, gateway или worker создает новый trace id.

## Поиск логов по trace id

1. В браузере или API client посмотрите response header `x-trace-id`.
2. Откройте Grafana -> Explore -> Loki.
3. Выполните поиск по значению trace id или по label `Application`, если dashboard настроен на labels.
4. Сравните gateway request log, gRPC call log, RabbitMQ publish/consume log и handler log.

## Проверка readiness

Перед release проверьте, что `/health/ready` не зеленый при отсутствующих production secrets, недоступной БД, недоступном RabbitMQ или неверных адресах внутренних сервисов.
