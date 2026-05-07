# user-authentication

Сервис отвечает за аккаунты пользователей, регистрацию, login, external provider login, sessions, refresh/logout, passwords, Email MFA и TOTP MFA.

## gRPC/HTTP API

Gateway публикует эти gRPC services как HTTP JSON endpoints:

- `RegistrationService` - register, complete registration, resend code.
- `AuthenticationService` - login by password, login by external provider.
- `PasswordService` - change, set, reset, confirm reset.
- `MfaService` - add/confirm/remove factors, backup codes.
- `MfaSessionService` - start, status, send email code, verify code.
- `UserSessionService` - refresh, logout, logout all, sessions.
- `LinkedProviderService` - link/unlink/list providers.

## RabbitMQ

Публикует events:

- `user.authentication.registered`
- `user.authentication.email-confirmed`
- `user.authentication.mfa-email-code-generated`
- `user.authentication.password-reset-requested`

Consumers отсутствуют.

## Зависимости

- PostgreSQL.
- RabbitMQ.
- External provider Yandex.
- Notification service через RabbitMQ events.

## Env/settings

- `ConnectionStrings__DefaultConnection`
- `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes`
- `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`, `RabbitMq__ExchangeName`
- `TotpEncryption__Key`
- `Totp__Issuer`, `Totp__VerificationWindowPastSteps`, `Totp__VerificationWindowFutureSteps`
- `ExternalProviders__Yandex__ClientId`, `ExternalProviders__Yandex__ClientSecret`, `ExternalProviders__Yandex__RedirectUri`

Secret values не писать в README или logs.

## Health/ready/metrics

- `/health/live`
- `/health/ready`
- metrics публикуются для Prometheus на настроенном metrics endpoint/port.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```

Для локальной отладки выберите `debug.user.authentication.yml`, затем запускайте API из IDE или `dotnet run`, оставив PostgreSQL/RabbitMQ в compose.
