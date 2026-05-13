# web-client

Vue 3 + Vite frontend Mazy Platform. Приложение дает пользователю UI для регистрации и входа, MFA, проектов, редактора сценариев, публикации release-версий, управления ботами и настройками.

## API

Frontend работает с gateway через HTTP JSON API `/api/v1/*`. В dev режиме Vite proxy отправляет `/api` на `http://localhost:8080`. В container режиме nginx проксирует API к compose service `gateway`.

Основные API-группы:

- `/api/v1/auth/*` - registration и login;
- `/api/v1/password/*` - set/change/reset password;
- `/api/v1/mfa/*` - MFA factors и sessions;
- `/api/v1/sessions/*` - refresh/logout/sessions;
- `/api/v1/projects/*` - projects, scenario draft/release, schemas, user data;
- `/api/v1/bots/*` - bot lifecycle и scenario version switching.

## Зависимости

- Node.js версии из `package.json` engines.
- npm.
- Gateway на `http://localhost:8080` для локального API.

## Env/settings

- `VITE_API_URL` - base URL API. Для dev proxy может быть пустым.
- `VITE_YANDEX_CLIENT_ID` - public client id для provider login.

Не храните в frontend env secrets. Все `VITE_*` значения попадают в client bundle.

## Команды

```powershell
npm install
npm run dev
npm run build
npm run type-check
npm run smoke
npm run lint
```

`npm run smoke` выполняет production build, поднимает `vite preview` на `127.0.0.1:4173` и проверяет, что собранный app shell отдается по HTTP.

## Запуск

Локально:

```powershell
cd apps\web-client
npm install
npm run dev
```

Через compose:

```powershell
.\tools\start.cmd
```

Frontend доступен на `http://localhost:5173`.
