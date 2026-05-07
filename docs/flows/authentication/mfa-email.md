# Email MFA

## Назначение

Отправить email-код для MFA session и подтвердить factor. Для login используется unauthenticated ветка, потому что tokens еще не выданы. Для защищенных действий используется authenticated ветка.

## Источники истины

- `mfa_session.proto`: `/api/v1/mfa/sessions/*`.
- `MfaSessionServiceProxy`: authenticated `SendEmailCode/VerifyCode`, anonymous `SendUnauthenticatedEmailCode/VerifyUnauthenticatedCode`.
- `SendUnauthenticatedEmailCodeHandler`: создает `OneTimePassword` типа `MfaEmail`.
- `SendOtpOnOneTimePasswordCreatedHandler`: публикует `user.authentication.mfa-email-code-generated`.
- `OneTimePassword.Create`: добавляет `OneTimePasswordCreatedDomainEvent` с открытым кодом только внутри доменного события; в БД хранится hash.
- `UserAccountMfaEmailCodeGeneratedIntegrationEventHandler`: отправляет email.

## Предусловия

- `MfaSession` уже создан login/set-password/start flow.
- У пользователя есть email MFA factor или email доступен как MFA method.
- Notification service слушает `notification.user` (`user.#`).

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
    participant PG as PostgreSQL
    participant MQ as RabbitMQ mazy.exchange
    participant NOTIF as notification
    participant SMTP as Email provider

    FE->>GW: HTTP POST /api/v1/mfa/sessions/{id}/email/send-unauthenticated
    GW->>GW: AllowAnonymous + PublicAuth rate limit
    GW->>AUTH: gRPC MfaSessionService.SendUnauthenticatedEmailCode
    AUTH->>PG: Get unauthenticated MfaSession
    AUTH->>PG: Add OneTimePassword(type=MfaEmail, hashed code)
    AUTH->>PG: UnitOfWork.SaveChangesAsync
    AUTH->>AUTH: Dispatch OneTimePasswordCreatedDomainEvent
    AUTH->>PG: Load UserAccount for target email
    AUTH--)MQ: user.authentication.mfa-email-code-generated
    MQ--)NOTIF: queue notification.user, routing user.#
    NOTIF->>SMTP: SendAsync(to, subject, code body)
    SMTP-->>U: Email code
    U->>FE: Ввод кода
    FE->>GW: HTTP POST /api/v1/mfa/sessions/{id}/verify-unauthenticated
    GW->>AUTH: gRPC MfaSessionService.VerifyUnauthenticatedCode
    AUTH->>PG: VerifyMfaFactorService
    AUTH->>PG: UnitOfWork.SaveChangesAsync
    AUTH->>AUTH: Dispatch MfaSessionCompletedDomainEvent if completed
    Note over AUTH: Для completed MFA нет RabbitMQ publisher handler
    alt Все required factors подтверждены
        AUTH-->>GW: VerifyUnauthenticatedCodeResponse { isCompleted=true }
        GW-->>FE: 200 OK
    else Требуются другие factors
        AUTH-->>GW: VerifyUnauthenticatedCodeResponse { isCompleted=false }
        GW-->>FE: 200 continue MFA
    end
```

## Транспорты

- HTTP: `/api/v1/mfa/sessions/{mfa_session_id}/email/send-unauthenticated`, `/verify-unauthenticated`.
- gRPC: `MfaSessionService.SendUnauthenticatedEmailCode`, `VerifyUnauthenticatedCode`.
- RabbitMQ: `OneTimePasswordCreatedDomainEvent` -> `user.authentication.mfa-email-code-generated` -> `notification.user`.
- Email provider: `notification -> SMTP/API`.

## `x-trace-id`

Trace id проходит HTTP -> gRPC -> RabbitMQ headers -> notification logging scope. Запрос verify обычно отдельный и может иметь другой trace id.

## Возможные ошибки

- MFA session не найдена или не unauthenticated;
- OTP истек или неверен;
- RabbitMQ недоступен;
- notification consumer не активен;
- email provider недоступен.

## Локальная проверка

1. Получить MFA challenge через login password.
2. Вызвать send unauthenticated email code.
3. Проверить event `user.authentication.mfa-email-code-generated` и logs notification.
4. Ввести код и проверить `isCompleted`.
