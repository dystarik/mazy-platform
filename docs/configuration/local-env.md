# Локальная конфигурация

Локальное окружение использует `infra/dev/.env`. Значения из этого файла не документируются и не должны копироваться в Markdown. Ниже приведены только имена переменных и назначение.

## Общие

- `ASPNETCORE_ENVIRONMENT` - окружение .NET сервисов.
- `CORS_ALLOWED_ORIGIN` - origin frontend для gateway CORS.

## PostgreSQL

- `POSTGRES_USER` - пользователь PostgreSQL.
- `POSTGRES_PASSWORD` - пароль PostgreSQL, secret.
- `POSTGRES_DB_AUTH` - БД user-authentication.
- `POSTGRES_DB_SCENARIO` - БД scenario-repository.
- `POSTGRES_DB_BOT` - БД bot-manager.

## MongoDB

- `MONGO_USER` - пользователь MongoDB.
- `MONGO_PASSWORD` - пароль MongoDB, secret.
- `MONGO_DB_SCENARIO` - БД для scenario runtime/storage.

## RabbitMQ

- `RABBITMQ_USER` - пользователь RabbitMQ.
- `RABBITMQ_PASSWORD` - пароль RabbitMQ, secret.

## Security

- `JWT_SECRET_KEY` - secret для подписи JWT.
- `TOTP_ENCRYPTION_KEY` - ключ шифрования TOTP secrets.
- `BOT_TOKEN_ENCRYPTION_KEY` - ключ шифрования bot tokens.
- `INTERNAL_API_TOKEN_SCENARIO_REPOSITORY` - token для internal scenario-repository API.
- `INTERNAL_API_TOKEN_BOT_MANAGER` - token для internal bot-manager API.

## Email

- `EMAIL_USERNAME` - логин SMTP/email provider.
- `EMAIL_PASSWORD` - пароль SMTP/email provider, secret.

## External providers

- `ExternalProviders_Yandex_ClientId` - public client id для Yandex provider.
- `ExternalProviders_Yandex_ClientSecret` - Yandex client secret.
- `ExternalProviders_Yandex_RedirectUri` - redirect URI.

## Bot providers

- `VK_API_VERSION` - версия VK API.
- `VK_WAIT_SECONDS` - wait timeout для VK long polling.

## Observability

- `GRAFANA_USER` - локальный пользователь Grafana.
- `GRAFANA_PASSWORD` - пароль Grafana, secret.

## Frontend

`apps/web-client/.env`:

- `VITE_API_URL` - base URL API; для dev proxy может быть пустым.
- `VITE_YANDEX_CLIENT_ID` - public client id для frontend provider flow.

`apps/web-client/.env.production`:

- `VITE_API_URL` - production API URL или пустое значение при reverse proxy.
