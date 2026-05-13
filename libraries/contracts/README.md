# contracts

Общие контракты Mazy Platform: protobuf/gRPC services, HTTP JSON transcoding annotations, DTO/events и базовые interfaces для RabbitMQ integration events. Любое изменение здесь почти всегда требует проверки нескольких сервисов.

## Где смотреть код

- `src/MazyPlatform.Contracts.Core` - `IIntegrationEvent`, handlers/dispatcher contracts, `IntegrationEventTypeAttribute`.
- `src/MazyPlatform.Contracts.User` и `src/MazyPlatform.Contracts.User.Grpc` - user events и authentication API.
- `src/MazyPlatform.Contracts.Scenario` и `src/MazyPlatform.Contracts.Scenario.Grpc` - scenario events и repository API.
- `src/MazyPlatform.Contracts.Bot` и `src/MazyPlatform.Contracts.Bot.Grpc` - bot events и manager API.

Потребители: gateway, backend-сервисы, workers и web-client через API schema/generated types.

## Как проверять изменения

Минимально:

```powershell
dotnet build libraries\contracts\MazyPlatform.Contracts.slnx
```

После изменения protobuf или events проверьте зависящий компонент:

- user contracts -> `services/user/authentication`, `services/notification`, gateway auth proxies, frontend auth/session types;
- scenario contracts -> `services/scenario/repository`, `services/scenario/engine`, `services/bot/manager`, frontend project/scenario types;
- bot contracts -> `services/bot/manager`, `services/bot/integration`, `services/scenario/engine`, frontend bot types;
- core event contracts -> все RabbitMQ producers/consumers.

## Правила совместимости

- Не менять protobuf field numbers существующих полей.
- Новые поля добавлять backward-compatible способом.
- Не переиспользовать удаленные field numbers.
- Не менять `/api/v1/*` annotations без обновления gateway/frontend/docs.
- Не переименовывать routing keys без миграции producers/consumers.
- Event payload должен оставаться десериализуемым старыми consumers, пока они могут быть в контуре.

## Рискованные изменения

- Изменение namespace/package/service names ломает generated gRPC clients.
- Переименование enum values может сломать сохраненные состояния и frontend mappings.
- Удаление поля из event безопасно только после проверки всех consumers.
- Contract build сам по себе недостаточен: нужен build/test сервисов, которые используют измененный пакет.
