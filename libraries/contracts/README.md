# contracts

Общая библиотека контрактов Mazy Platform: protobuf/gRPC services, HTTP JSON transcoding annotations, DTO/events и базовые interfaces для RabbitMQ integration events.

## Используют

- `edge/gateway`
- `services/user/authentication`
- `services/scenario/repository`
- `services/scenario/engine`
- `services/bot/manager`
- `services/bot/integration`
- `services/notification`
- `apps/web-client` через сгенерированные client types/API schema

## Основные модули

- `MazyPlatform.Contracts.Core` - `IIntegrationEvent`, `IIntegrationEventHandler`, dispatcher contracts, `IntegrationEventTypeAttribute`.
- `MazyPlatform.Contracts.User` и `MazyPlatform.Contracts.User.Grpc` - user events и authentication API.
- `MazyPlatform.Contracts.Scenario` и `MazyPlatform.Contracts.Scenario.Grpc` - scenario events и repository API.
- `MazyPlatform.Contracts.Bot` и `MazyPlatform.Contracts.Bot.Grpc` - bot events и manager API.

## Важно при изменениях

- Не менять protobuf field numbers для существующих полей.
- Не переименовывать routing keys без миграции consumers/producers.
- Не ломать `/api/v1/*` annotations без обновления frontend и docs.
- Добавлять новые поля backwards-compatible способом.
- Проверять все сервисы, которые ссылаются на обновленный contract package.
