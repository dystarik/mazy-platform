# Подтверждение email

## Назначение

Отправить код подтверждения регистрации и завершить подтверждение email. Notification отправляет письмо только по `user.authentication.registered`; после успешного подтверждения user-authentication публикует `user.authentication.email-confirmed` для других сервисов.

## Источники истины

- `registration.proto`: `POST /api/v1/auth/registration/password`, `/complete`, `/resend`.
- `RegisterByPasswordHandler`: создает account и OTP email confirmation.
- `SendOtpOnOneTimePasswordCreatedHandler`: publishes `user.authentication.registered`.
- `UserAccountRegisteredIntegrationEventHandler`: sends email.
- `CompleteRegistrationHandler` + `UserAccountEmailVerifiedHandler`: publishes `user.authentication.email-confirmed`.
- `UnitOfWork.SaveChangesAsync`: после сохранения достает domain events из `UserAccount` и `OneTimePassword`; именно `OneTimePasswordCreatedDomainEvent` запускает письмо с кодом.

## Предусловия

- Email еще не зарегистрирован.
- RabbitMQ и notification запущены.
- Email provider credentials настроены.

## Участники

Пользователь, web-client, gateway, user-authentication, PostgreSQL, RabbitMQ, notification, email provider, scenario-repository, bot-manager.

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
    participant SR as scenario-repository
    participant BM as bot-manager

    U->>FE: Registration form
    FE->>GW: HTTP POST /api/v1/auth/registration/password
    GW->>GW: AllowAnonymous + PublicAuth rate limit
    GW->>AUTH: gRPC RegistrationService.RegisterByPassword
    AUTH->>PG: Create UserAccount + OneTimePassword(EmailConfirmation hash)
    AUTH->>PG: UnitOfWork.SaveChangesAsync
    AUTH->>AUTH: Dispatch UserAccountRegisteredByPasswordDomainEvent
    AUTH->>AUTH: Dispatch OneTimePasswordCreatedDomainEvent
    AUTH->>PG: Load UserAccount for email
    AUTH--)MQ: user.authentication.registered
    MQ--)NOTIF: queue notification.user, routing user.#
    NOTIF->>SMTP: Send confirmation email with code
    SMTP-->>U: Email code
    AUTH-->>GW: RegisterByPasswordResponse
    GW-->>FE: 200 OK

    U->>FE: Ввод confirmation code
    FE->>GW: HTTP POST /api/v1/auth/registration/complete
    GW->>AUTH: gRPC RegistrationService.CompleteRegistration
    AUTH->>PG: Verify OTP, mark email verified
    AUTH->>PG: UnitOfWork.SaveChangesAsync
    AUTH->>AUTH: Dispatch UserAccountEmailVerifiedDomainEvent
    AUTH--)MQ: user.authentication.email-confirmed
    MQ--)SR: queue scenario-repository.user-account.email-confirmed
    MQ--)BM: queue bot-manager.user-account.email-confirmed
    AUTH-->>GW: CompleteRegistrationResponse
    GW-->>FE: 200 OK
```

## Транспорты

- HTTP: `/api/v1/auth/registration/password`, `/api/v1/auth/registration/complete`.
- gRPC: `RegistrationService.RegisterByPassword`, `CompleteRegistration`.
- RabbitMQ: `OneTimePasswordCreatedDomainEvent(EmailConfirmation)` -> `user.authentication.registered`; `UserAccountEmailVerifiedDomainEvent` -> `user.authentication.email-confirmed`.
- Email provider: notification -> SMTP/API.

## `x-trace-id`

Registration request trace id попадает в `user.authentication.registered`. Complete request может иметь другой trace id и попадает в `user.authentication.email-confirmed`.

## Возможные ошибки

- email уже занят;
- password невалиден;
- confirmation code истек или неверен;
- RabbitMQ publish failed;
- notification не может отправить email;
- downstream consumers email-confirmed упали и сообщение ушло в DLQ.

## Локальная проверка

1. Зарегистрировать пользователя.
2. Проверить event `user.authentication.registered`.
3. Проверить logs notification.
4. Завершить регистрацию.
5. Проверить event `user.authentication.email-confirmed` и consumers scenario-repository/bot-manager.
