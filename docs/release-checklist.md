# Чеклист release v1.0.0

## Сборка

- Собрать `apps/web-client`: `npm run build`.
- Собрать backend проекты через соответствующие `.sln` или `.csproj`.
- Собрать Docker images из `infra/dev/docker-compose.yml`.

## Тесты

- Запустить unit tests библиотек и сервисов.
- Проверить frontend type-check.
- Проверить lint/format там, где это часть release gate.

## Smoke-проверка Docker Compose

- Запустить `.\tools\start.cmd` без debug overlays.
- Проверить, что миграционные контейнеры завершились успешно.
- Проверить `gateway`, `web-client`, `authentication`, `scenario-repository`, `scenario-engine`, `bot-manager`, `bot-integration`, `notification`.

## Проверка secrets

- Убедиться, что реальные secrets не попали в git.
- Проверить production значения для JWT, SMTP, provider client secret, RabbitMQ, PostgreSQL, MongoDB, internal API tokens, bot token encryption и TOTP encryption.
- Проверить rotation policy для production secrets.

## Logs, metrics, health

- Проверить `/health/live`, `/health/ready`, `/metrics`.
- Проверить, что Prometheus собирает metrics.
- Проверить, что Loki принимает logs.
- Проверить поиск по `x-trace-id`.

## Smoke-проверка frontend

- Открыть landing/login.
- Зарегистрировать или залогинить тестового пользователя.
- Создать проект.
- Сохранить и провалидировать draft сценария.
- Опубликовать release.
- Подключить тестового бота без публикации реального token в logs.

## Проверка production-конфигурации

- CORS origins соответствуют production frontend.
- Gateway смотрит на production service addresses.
- В production отключены dev-only anonymous/admin режимы.
- Monitoring endpoints защищены сетевыми правилами.
- External provider redirect URI совпадает с production callback.
