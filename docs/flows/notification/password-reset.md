# Сброс пароля

## Назначение

Создать OTP для сброса пароля, отправить код по email и затем установить новый password hash после подтверждения кода.

## Источники истины

- `password.proto`: `POST /api/v1/password/reset`, `/reset/confirm`.
- `PasswordServiceProxy.ResetPassword` и `ConfirmResetPassword`: `[AllowAnonymous]`, rate limit `PublicAuth`.
- `ResetPasswordHandler`: создает password reset OTP.
- `SendOtpOnOneTimePasswordCreatedHandler`: publishes `user.authentication.password-reset-requested`.
- `UserAccountPasswordResetRequestedIntegrationEventHandler`: sends email.
- `ConfirmResetPasswordHandler`: проверяет OTP и меняет password.
- `UnitOfWork.SaveChangesAsync`: `OneTimePasswordCreatedDomainEvent` появляется при создании OTP и уже после сохранения приводит к RabbitMQ publish.

## Предусловия

- Аккаунт с email существует и может сбрасывать пароль.
- RabbitMQ и notification запущены.
- Email provider доступен.

## Участники

Пользователь, web-client, gateway, user-authentication, PostgreSQL, RabbitMQ, notification, email provider.

## Диаграмма

```mermaid
sequenceDiagram
    autonumber
    actor U as Пользователь
    participant FE as web-client
    participant GW as gateway
    participant AUTH as user-authentication
    participant PG as PostgreSQL auth
    participant MQ as RabbitMQ mazy.exchange
    participant NOTIF as notification
    participant SMTP as Email provider

    U->>FE: Запрос password reset
    FE->>GW: HTTP POST /api/v1/password/reset
    GW->>GW: AllowAnonymous + PublicAuth rate limit
    GW->>AUTH: gRPC PasswordService.ResetPassword
    AUTH->>PG: Find account, create OneTimePassword(PasswordReset hash)
    AUTH->>PG: UnitOfWork.SaveChangesAsync
    AUTH->>AUTH: Dispatch OneTimePasswordCreatedDomainEvent
    AUTH->>PG: Load UserAccount for email
    AUTH--)MQ: user.authentication.password-reset-requested
    MQ--)NOTIF: queue notification.user, routing user.#
    NOTIF->>SMTP: Send reset email with code
    SMTP-->>U: Reset code
    AUTH-->>GW: ResetPasswordResponse
    GW-->>FE: 200 OK

    U->>FE: Ввод кода и нового пароля
    FE->>GW: HTTP POST /api/v1/password/reset/confirm
    GW->>AUTH: gRPC PasswordService.ConfirmResetPassword
    AUTH->>PG: Verify OTP, validate new password, save password hash
    AUTH->>PG: UnitOfWork.SaveChangesAsync
    AUTH->>AUTH: Dispatch UserAccountPasswordChangedDomainEvent if raised
    Note over AUTH: Для password changed нет RabbitMQ publisher handler
    AUTH-->>GW: Empty success
    GW-->>FE: 200 OK
```

## Транспорты

- HTTP: `/api/v1/password/reset`, `/api/v1/password/reset/confirm`.
- gRPC: `PasswordService.ResetPassword`, `ConfirmResetPassword`.
- RabbitMQ: `OneTimePasswordCreatedDomainEvent(PasswordReset)` -> `user.authentication.password-reset-requested`. Confirm reset в RabbitMQ не публикует событие.
- Email provider: notification -> SMTP/API.

## `x-trace-id`

Reset request trace id идет в RabbitMQ event. Confirm request обычно отдельный и может иметь другой trace id.

## Возможные ошибки

- email не найден или account state запрещает reset;
- OTP истек или неверен;
- новый пароль невалиден;
- RabbitMQ publish failed;
- notification/email provider failed;
- PostgreSQL недоступен.

## Локальная проверка

1. На экране reset password запросить код.
2. Проверить event `user.authentication.password-reset-requested`.
3. Проверить logs notification.
4. Подтвердить reset кодом и новым паролем.
5. Войти новым паролем.
