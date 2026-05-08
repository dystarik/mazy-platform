# Секреты

Секрет - любое значение, позволяющее получить доступ к данным, инфраструктуре, внешнему API или подписывать/расшифровывать защищенную информацию.

## Что является secret

- `POSTGRES_PASSWORD`
- `MONGO_PASSWORD`
- `RABBITMQ_PASSWORD`
- `EMAIL_PASSWORD`
- `JWT_SECRET_KEY` / `Jwt__SecretKey`
- `TOTP_ENCRYPTION_KEY` / `TotpEncryption__Key`
- `BOT_TOKEN_ENCRYPTION_KEY` / `BotTokenEncryption__Key`
- `INTERNAL_API_TOKEN_SCENARIO_REPOSITORY`
- `INTERNAL_API_TOKEN_BOT_MANAGER`
- `InternalApi__AccessToken`
- `BotManager__AccessToken`
- `ScenarioRepository__AccessToken`
- `ExternalProviders__Yandex_ClientSecret` / `ExternalProviders__Yandex__ClientSecret`
- `NUGET_API_KEY`
- реальные bot tokens VK/Telegram.

## Правила

- Не хранить реальные secrets в Markdown.
- Не добавлять secrets в examples, screenshots, logs snippets и issue comments.
- Не использовать production secrets локально.
- Не переиспользовать один secret для разных назначений.
- При утечке считать secret скомпрометированным и ротировать.
- В logs вместо secrets писать только тип настройки или redacted marker.

## Что можно документировать

- имя переменной;
- назначение;
- сервис-владелец;
- является ли значение secret;
- где значение должно быть задано;
- формат на уровне описания, без реального значения и без production connection string.
