# notification

Worker отправки email-уведомлений. Сервис слушает user authentication events и отправляет письма через настроенный email provider.

## API/consumers

Public API отсутствует.

Consumers:

- `user.authentication.registered`
- `user.authentication.mfa-email-code-generated`
- `user.authentication.password-reset-requested`

Queue подписана на routing key `user.#`.

## Зависимости

- RabbitMQ.
- SMTP/email provider.

## Env/settings

- `RabbitMq__Host`
- `RabbitMq__Username`
- `RabbitMq__Password`
- `RabbitMq__ExchangeName`
- `RabbitMq__Queues__*`
- `Email__Username`
- `Email__Password`

`Email__Password` и RabbitMQ password являются secrets.

## Health/ready/metrics

Это worker service. Проверяйте:

- наличие consumer на очереди `notification.user`;
- logs отправки email;
- DLQ для неуспешных сообщений;
- metrics на `MetricsPort`.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```
