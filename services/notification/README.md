# notification

Worker email-уведомлений. Слушает user authentication events в RabbitMQ и отправляет письма через настроенный SMTP/email provider.

## Где смотреть код

- `src/MazyPlatform.Service.Notification/Program.cs` - DI, hosted services, RabbitMQ, metrics.
- `Messaging/RabbitMqConsumerService.cs` - подписка, retry, DLQ.
- `Messaging/SmtpEmailSender.cs` - SMTP отправка.
- `IntegrationEventHandlers` - обработчики registration, MFA email code, password reset и шаблоны писем.
- `Observability` - trace/config diagnostics.
- `src/MazyPlatform.Service.Notification/appsettings.json` - имена RabbitMQ/Email настроек.

## Запуск и отладка

```powershell
.\tools\start.cmd
```

Отдельного debug overlay для notification нет. Для локального запуска оставьте RabbitMQ в compose и задайте email/RabbitMQ настройки окружения:

```powershell
dotnet run --project services\notification\src\MazyPlatform.Service.Notification\MazyPlatform.Service.Notification.csproj
```

Важные настройки по именам:

- `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`, `RabbitMq__ExchangeName`
- `RabbitMq__Queues__*`
- `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__From`, `Email__DisplayName`

`Email__Password` и `RabbitMq__Password` - secrets; значения не писать в README и logs.

## Что проверять

```powershell
dotnet build services\notification\MazyPlatform.Service.Notification.slnx
```

Ручная проверка через RabbitMQ Management (`http://localhost:15672`):

- queue `notification.user` создана и подписана на `user.#`;
- consumer подключен после старта worker;
- registration/password reset/MFA events обрабатываются;
- неуспешные сообщения уходят в `notification.user.dlx`;
- в logs нет email passwords, provider credentials и лишнего содержимого кодов.

Metrics доступны на `MetricsPort` внутри compose и собираются Prometheus.

## События

Слушает события:

- `user.authentication.registered`
- `user.authentication.mfa-email-code-generated`
- `user.authentication.password-reset-requested`

## Рискованные изменения

- Изменения routing key `user.#` или queue name ломают доставку писем.
- Retry/DLQ настройки влияют на повторную отправку писем; проверяйте дубли.
- Шаблоны писем не должны раскрывать секреты или внутренние identifiers.
- SMTP provider limits могут проявиться только в long-running режиме.
