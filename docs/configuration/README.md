# Конфигурация

Раздел описывает настройки окружений и secrets. Здесь перечислены имена переменных и назначение, но не реальные значения.

- [local-env.md](local-env.md) - локальные `.env` файлы.
- [production-env.md](production-env.md) - production-переменные и требования.
- [secrets.md](secrets.md) - что считается secret и как с ним обращаться.
- [service-settings.md](service-settings.md) - настройки по сервисам.

## Источники конфигурации

- `infra/dev/.env` - локальное compose окружение.
- `apps/web-client/.env` и `apps/web-client/.env.production` - frontend build/runtime настройки.
- `tools/packages/.env` - настройки публикации NuGet packages.
- `appsettings.json` внутри backend-сервисов - структура настроек и безопасные defaults.
- переменные окружения с `__` для override вложенных .NET settings.

## Запрет

Не добавляйте в git реальные значения:

- пароли;
- JWT secret;
- SMTP password;
- OAuth/provider client secret;
- internal API tokens;
- bot tokens;
- encryption keys;
- production connection strings;
- ключи NuGet или других package registries.
