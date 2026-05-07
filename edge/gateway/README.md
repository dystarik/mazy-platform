# gateway

Gateway - публичная backend-точка Mazy Platform. Он принимает HTTP JSON API и gRPC-web/JSON-transcoded запросы от frontend, проверяет JWT, применяет CORS/rate limiting, прокидывает `x-trace-id` и вызывает backend-сервисы по gRPC.

## API

Публикует HTTP JSON endpoints из protobuf annotations:

- user/authentication: `/api/v1/auth/*`, `/api/v1/password/*`, `/api/v1/mfa/*`, `/api/v1/sessions/*`, `/api/v1/linked-providers/*`;
- scenario-repository: `/api/v1/projects/*`, `/api/v1/schemas/*`, `/api/v1/nodes/catalog`, `/api/v1/projects/{project_id}/scenario/*`;
- bot-manager: `/api/v1/bots/*`.

gRPC proxy services:

- `AuthenticationService`, `RegistrationService`, `PasswordService`, `MfaService`, `MfaSessionService`, `UserSessionService`, `LinkedProviderService`;
- `ProjectService`, `EntitySchemaService`, `ScenarioGraphService`, `UserDataService`;
- `BotService`.

## Зависимости

- `authentication` gRPC endpoint.
- `scenario-repository` gRPC endpoint.
- `bot-manager` gRPC endpoint.
- JWT signing secret для validation.
- Data Protection keys volume в compose.

## Env/settings

- `Jwt__SecretKey` - secret для JWT validation.
- `Cors__AllowedOrigins__0` - разрешенный frontend origin.
- `Services__Authentication` - адрес user-authentication.
- `Services__ScenarioRepository` - адрес scenario-repository.
- `Services__BotManager` - адрес bot-manager.

## Health/ready/metrics

- `GET /health/live`
- `GET /health/ready`
- `GET /metrics`
- Swagger доступен через gateway, если включен в окружении.

## Запуск

Через compose:

```powershell
.\tools\start.cmd
```

Локально из проекта можно запускать .NET API, подняв зависимости через `infra/dev` и задав service addresses на локальные или compose endpoints.
