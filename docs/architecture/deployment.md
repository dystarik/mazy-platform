# Развертывание

Mazy Platform разворачивается набором независимых единиц поставки:

- `web-client` - статический frontend под nginx или другим web-сервером.
- `gateway` - публичный edge API.
- `authentication` - домен пользователей.
- `scenario-repository` - metadata сценариев и versions.
- `scenario-engine` - worker исполнения сценариев.
- `bot-manager` - bot metadata и credentials.
- `bot-integration` - worker внешних bot provider.
- `notification` - worker email-уведомлений.
- инфраструктурные зависимости: PostgreSQL, MongoDB, RabbitMQ, monitoring stack.

## Границы

Gateway - единственная публичная backend-точка для frontend. Сервисные gRPC endpoints должны оставаться внутренними. Internal gRPC endpoints, например `BotInternalService` и `ScenarioRepositoryInternalService`, защищаются internal API tokens и сетевой доступностью.

## Production-требования

- Secrets поставляются через secret manager или защищенные переменные окружения.
- Monitoring endpoints доступны только доверенной сети.
- Внешние provider redirect URI указывают на production frontend/API.
- `x-trace-id` сохраняется на HTTP, gRPC и RabbitMQ границах.
- Миграции БД выполняются контролируемо до старта сервисов или отдельным job.
