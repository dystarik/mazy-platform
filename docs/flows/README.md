# End-to-end flows

Эти документы нужны, когда меняется поведение между несколькими компонентами: HTTP endpoint, gRPC call, handler, сохранение состояния, domain event, RabbitMQ event или worker consumer.

Каждый flow показывает:

- какой пользовательский или системный путь описан;
- какие классы/контракты являются источниками истины;
- какие сервисы и хранилища участвуют;
- где проходит граница HTTP, gRPC и RabbitMQ;
- как идет `x-trace-id`;
- какие ошибки ожидаемы;
- как проверить flow локально.

## Как читать диаграммы

- `->>` - синхронный HTTP/gRPC вызов.
- `--)` - асинхронное RabbitMQ событие.
- `alt/else` - ветвление результата.
- `rect` - граница сервиса или инфраструктуры.
- `Note over` - правило, которое важно для реализации.

Диаграмма должна совпадать с кодом: proto annotation, gateway proxy, handler, domain method, `UnitOfWork.SaveChangesAsync`, domain event handler и routing key. Если handler сохраняет состояние без RabbitMQ event, это тоже должно быть видно.

## Когда обновлять flow

Обновляйте flow вместе с кодом, если меняется:

- HTTP path или protobuf message;
- gRPC service/rpc;
- порядок сохранения и публикации events;
- routing key или event payload;
- условие MFA, release promotion, bot activation, version switching;
- обработка `x-trace-id`;
- локальная проверка или типичная ошибка.

## Аутентификация

- [Регистрация по паролю](authentication/registration-password.md)
- [Вход по паролю](authentication/login-password.md)
- [Вход через provider](authentication/login-provider.md)
- [Установка пароля](authentication/set-password.md)
- [Email MFA](authentication/mfa-email.md)
- [TOTP MFA](authentication/mfa-totp.md)

## Сценарии

- [Валидация черновика](scenario/validate-draft.md)
- [Публикация release](scenario/publish-release.md)
- [Удаление версии](scenario/delete-version.md)

## Боты

- [Подключение бота](bot/connect-bot.md)
- [Входящее сообщение](bot/incoming-message.md)
- [Переключение версии сценария](bot/scenario-version-switching.md)

## Уведомления

- [Подтверждение email](notification/email-confirmation.md)
- [Сброс пароля](notification/password-reset.md)
