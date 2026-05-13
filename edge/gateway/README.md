# gateway

Gateway - публичная backend-точка Mazy Platform. Он принимает HTTP JSON API от `apps/web-client`, валидирует JWT, применяет CORS/rate limiting, прокидывает `x-trace-id` и вызывает backend-сервисы по gRPC.

## Где смотреть код

- `src/MazyPlatform.Service.Gateway/Program.cs` - сборка приложения и middleware.
- `src/MazyPlatform.Service.Gateway/Configuration` - CORS, rate limits, адреса сервисов, health checks.
- `src/MazyPlatform.Service.Gateway/Proxies` - HTTP JSON endpoints, которые проксируют gRPC-сервисы authentication, scenario-repository и bot-manager.
- `src/MazyPlatform.Service.Gateway/Interceptors` - trace/logging/header propagation.
- `src/MazyPlatform.Service.Gateway/appsettings.json` - имена настроек; секреты задавайте через окружение.

## Локальный запуск и отладка

Обычный dev-контур запускается из корня:

```powershell
.\tools\start.cmd
```

Compose публикует gateway на `http://localhost:8080`. Swagger, health и metrics доступны через этот же порт, если включены в окружении.

Для запуска из IDE или `dotnet run` поднимите зависимости в compose и задайте адреса:

- `Services__Authentication`
- `Services__ScenarioRepository`
- `Services__BotManager`
- `Jwt__SecretKey`
- `Cors__AllowedOrigins__0`

В compose gateway ходит к `authentication:8081`, `scenario-repository:6101`, `bot-manager:5101`.

## Что проверять после изменений

- `GET http://localhost:8080/health/live`
- `GET http://localhost:8080/health/ready`
- `GET http://localhost:8080/metrics`
- login/refresh flow через frontend или `/api/v1/auth/*` и `/api/v1/sessions/*`;
- операции проекта: создать проект, сохранить draft, validate/promote scenario;
- операции ботов: создать, bind/unbind, activate/deactivate.

Минимальная сборка:

```powershell
dotnet build edge\gateway\MazyPlatform.Service.Gateway.slnx
```

## Рискованные изменения

- Protobuf HTTP annotations и пути `/api/v1/*`: их использует frontend.
- JWT validation, propagation `x-trace-id`, user id, refresh token id и client IP в gRPC metadata.
- Rate limiting/CORS: легко сломать локальный dev или production origin.
- Адреса public/internal gRPC: gateway должен ходить только на public endpoints сервисов.
- Data Protection keys volume в compose: влияет на стабильность защищенных данных между рестартами.

Секреты в README и logs не писать: `Jwt__SecretKey` и связанные значения окружения должны оставаться только именами настроек.
