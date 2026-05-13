# Документация Mazy Platform

Этот раздел держит рабочую документацию проекта: архитектуру, локальный запуск, compose-окружение, наблюдаемость и end-to-end flows.

## Начать с этих файлов

- [overview.md](overview.md) - продуктовая и системная карта: кто вызывает кого, где лежат данные, какие flows важны.
- [local-development.md](local-development.md) - запуск проекта, debug overlays, smoke checks и частые сбои.
- [repository-structure.md](repository-structure.md) - директории monorepo и их ownership.

## Инфраструктура

- [docker-compose.md](docker-compose.md) - состав `infra/dev`, контейнеры, порты, volumes и debug overlays.
- [observability.md](observability.md) - логи, метрики, health/readiness, Grafana, Loki, Prometheus и `x-trace-id`.

## Архитектура

- [architecture/README.md](architecture/README.md) - как читать архитектурный раздел.
- [architecture/services.md](architecture/services.md) - сервисы, их данные, gRPC/RabbitMQ связи и риски изменений.
- [architecture/libraries.md](architecture/libraries.md) - общие библиотеки и контракты совместимости.
- [architecture/deployment.md](architecture/deployment.md) - границы поставки и production-требования.

## End-to-end flows

- [flows/README.md](flows/README.md) - индекс flow-документов.
- `flows/authentication/*` - регистрация, login, MFA, установка пароля.
- `flows/scenario/*` - validate draft, publish release, delete version.
- `flows/bot/*` - подключение бота, входящие сообщения, переключение версии.
- `flows/notification/*` - email confirmation и password reset.

## Secrets

В документации можно писать имена настроек и назначение. Нельзя коммитить реальные токены, пароли, connection strings, SMTP password, JWT secret, bot tokens и encryption keys.
