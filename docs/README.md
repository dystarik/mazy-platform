# Документация Mazy Platform

Эта папка содержит подробную документацию платформы. Корневой `README.md` остается короткой стартовой страницей, а архитектура, локальный запуск, конфигурация и end-to-end flow описаны здесь.

## Разделы

- [overview.md](overview.md) - что делает платформа и какие возможности включает.
- [repository-structure.md](repository-structure.md) - структура monorepo.
- [local-development.md](local-development.md) - локальный запуск и рабочий цикл разработки.
- [docker-compose.md](docker-compose.md) - состав `infra/dev`, контейнеры, порты, health и metrics.
- [observability.md](observability.md) - logs, Loki, Grafana, Prometheus, health checks, readiness и `x-trace-id`.
- [architecture](architecture/README.md) - сервисы, библиотеки и deployment.
- [flows](flows/README.md) - end-to-end flow с Mermaid sequence diagrams.

## Правило для secrets

Документация перечисляет только имена настроек и их назначение. Реальные значения токенов, паролей, connection strings, SMTP password, JWT secret, bot tokens и encryption keys нельзя добавлять в git.
