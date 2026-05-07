# End-to-end flow платформы

Flow-документы оформлены в едином строгом формате: назначение, источники истины в коде, предусловия, участники, Mermaid sequence diagram, транспортные границы, `x-trace-id`, ошибки и локальная проверка.

## Нотация диаграмм

- `->>` - синхронный HTTP/gRPC вызов.
- `--)` - асинхронное RabbitMQ событие.
- `alt/else` - ветвление результата.
- `rect` - логическая граница сервиса или инфраструктуры.
- `Note over` - важное правило реализации.

Диаграммы строятся по фактическим handlers, proxies, proto annotations, domain methods, `UnitOfWork.SaveChangesAsync`, domain event handlers и RabbitMQ routing keys. Если flow меняет агрегат, в диаграмме явно показывается сохранение, dispatch доменных событий и только затем publish integration event. Если handler сохраняет состояние, но RabbitMQ-события нет, это тоже фиксируется явно.

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
