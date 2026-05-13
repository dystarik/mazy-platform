# user-authentication

Сервис аккаунтов: registration, login, external provider login, sessions, refresh/logout, password flows, Email MFA и TOTP MFA. Наружу он доступен через gateway, сам публикует user events в RabbitMQ.

## Где смотреть код

- `src/MazyPlatform.Service.User.Authentication.Api` - gRPC services, composition root, appsettings.
- `src/MazyPlatform.Service.User.Authentication.Application` - commands/handlers/validators для auth, sessions, passwords, MFA.
- `src/MazyPlatform.Service.User.Authentication.Domain` - user account/session/value objects.
- `src/MazyPlatform.Service.User.Authentication.Infrastructure` - PostgreSQL, RabbitMQ, JWT, TOTP encryption, external providers.
- `tests/MazyPlatform.Service.User.Authentication.Domain.Tests` - domain-level инварианты.
- `tests/MazyPlatform.Service.User.Authentication.Integration.Tests` - gRPC flows, RabbitMQ events, health.

## Запуск и отладка

Обычный контур:

```powershell
.\tools\start.cmd
```

Для локальной отладки выберите `debug.user.authentication.yml`, оставьте PostgreSQL/RabbitMQ в compose и запускайте API из IDE или:

```powershell
dotnet run --project services\user\authentication\src\MazyPlatform.Service.User.Authentication.Api\MazyPlatform.Service.User.Authentication.Api.csproj
```

Основные настройки по именам:

- `ConnectionStrings__DefaultConnection`
- `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes`
- `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`, `RabbitMq__ExchangeName`
- `TotpEncryption__Key`
- `Totp__Issuer`, `Totp__VerificationWindowPastSteps`, `Totp__VerificationWindowFutureSteps`
- `ExternalProviders__Yandex__ClientId`, `ExternalProviders__Yandex__ClientSecret`, `ExternalProviders__Yandex__RedirectUri`

## Что проверять

```powershell
dotnet test services\user\authentication\MazyPlatform.Service.User.Authentication.slnx
```

После изменений в API также проверьте через gateway:

- registration -> email confirmation event;
- login by password, refresh, logout, logout all;
- change/set/reset password;
- Email MFA и TOTP MFA;
- external provider login/link/unlink, если трогались provider настройки.

Health endpoints: `/health/live`, `/health/ready`. Metrics собирает Prometheus через настроенный metrics endpoint/port.

## События

Публикует:

- `user.authentication.registered`
- `user.authentication.email-confirmed`
- `user.authentication.mfa-email-code-generated`
- `user.authentication.password-reset-requested`

Сервис не слушает RabbitMQ events. Эти routing keys используют notification, scenario-repository и bot-manager; переименование требует миграции всех подписчиков.

## Рискованные изменения

- JWT claims/issuer/audience и refresh token id: завязаны gateway и frontend sessions.
- Хеширование/ротация refresh tokens и logout all: ошибки дают незаметные session leaks.
- TOTP encryption и backup codes: секреты не логировать, формат уже сохраненных данных не ломать.
- RabbitMQ events registration/email-confirmed: downstream сервисы создают свою проекцию пользователя.
- Password reset и MFA email codes: проверяйте TTL, повторную отправку и idempotency.
