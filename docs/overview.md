# Обзор платформы

Mazy Platform состоит из редактора сценариев, публичного gateway, backend-сервисов и workers для ботов. Главный пользовательский путь такой:

1. Пользователь регистрируется или входит в `apps/web-client`.
2. Создает проект, описывает схему данных и собирает draft-граф сценария.
3. Валидирует draft и публикует release-версию.
4. Подключает бота, привязывает его к проекту и выбирает версию сценария.
5. `bot-integration` получает события из VK/Telegram, `scenario-engine` исполняет опубликованный граф и отправляет действия обратно во внешнюю платформу.

## Карта системы

- `apps/web-client` вызывает только gateway по HTTP JSON API `/api/v1/*`.
- `edge/gateway` проверяет JWT, применяет CORS/rate limiting, прокидывает `x-trace-id` и вызывает backend по gRPC.
- `user-authentication` хранит аккаунты, сессии, password/MFA/external provider flows.
- `scenario-repository` хранит проекты, схемы данных, draft graph, release versions и user data records.
- `bot-manager` хранит bot instances, credentials, project binding и выбранную scenario version.
- `bot-integration` синхронизирует активных ботов из `bot-manager`, читает внешние события и публикует `bot.integration.incoming_event`.
- `scenario-engine` слушает bot events, загружает release graph и выполняет scenario runtime.
- `notification` слушает user events и отправляет email.

## Данные и транспорт

- HTTP JSON: внешний контракт `web-client -> gateway`. Пути задаются в protobuf annotations.
- gRPC: синхронные вызовы `gateway -> services` и внутренние service-to-service вызовы.
- RabbitMQ: integration events между доменами.
- PostgreSQL: metadata пользователей, сценариев и ботов.
- MongoDB: scenario graph/runtime documents.
- SMTP/email provider: подтверждение email, MFA-коды, reset password.
- VK/Telegram API: входящие bot events и исходящие сообщения.

## Основные flows

- Auth: регистрация, login по паролю, login через provider, Email MFA, TOTP MFA, refresh/logout.
- Scenario: draft, validation, release promotion, rollback/version history, delete version.
- Bot: create/bind/activate/deactivate bot, смена token, выбор версии сценария.
- Runtime: входящее сообщение от бота, загрузка release graph, выполнение nodes, отправка ответа.
- Notification: отправка писем по events из `user-authentication`.

Подробные sequence diagrams лежат в [flows](flows/README.md). Если меняете handler, protobuf endpoint, routing key или порядок сохранения/publish, сначала найдите соответствующий flow и обновите его вместе с кодом.
