# web-client

Vue 3 + Vite frontend Mazy Platform. Здесь пользователь проходит регистрацию/login/MFA, работает с проектами, редактором сценариев, release-версиями, ботами и настройками.

## Где смотреть код

- `src/main.ts` - bootstrap приложения.
- `src/router` - маршруты и guards.
- `src/views/auth` - registration, login, MFA, reset password, Yandex callback.
- `src/views/projects` и `src/views/scenarios` - проекты, user data и редактор сценариев.
- `src/views/bots` - управление bot instances.
- `src/stores` - Pinia stores для auth, sessions, projects, MFA, bots и т.д.
- `src/types/api` - типы API; сверяйте с `swagger.json` и контрактами gateway.
- `vite.config.ts` - dev proxy `/api -> http://localhost:8080`.

## Локальный запуск

```powershell
cd apps\web-client
npm install
npm run dev
```

Dev server: `http://localhost:5173`. Если `VITE_API_URL` пустой, запросы `/api` уходят через Vite proxy на gateway `http://localhost:8080`.

Через общий compose:

```powershell
.\tools\start.cmd
```

Контейнерный nginx проксирует API к compose service `gateway`; порт frontend задается `WEB_CLIENT_PORT`, по умолчанию `5173`.

## Команды сопровождения

```powershell
npm run type-check
npm run build
npm run smoke
npm run lint
```

`npm run smoke` делает production build, поднимает `vite preview` на `127.0.0.1:4173` и проверяет, что app shell отдается по HTTP. `lint` запускает fix-режим, поэтому перед ним убедитесь, что правки ограничены вашей задачей.

Node.js берите из `package.json` `engines`: `^20.19.0 || >=22.12.0`.

## Что проверять руками

- login, refresh после перезагрузки страницы, logout;
- MFA flow: start session, verify code, backup/TOTP состояния;
- создание проекта, сохранение draft, validate, promote/release;
- node catalog и совместимость редактора с backend validation;
- создание бота, binding к project, выбор scenario version, activate/deactivate;
- Yandex callback, если менялись provider routes/env.

## Рискованные изменения

- Все `VITE_*` попадает в client bundle: secrets сюда не добавлять.
- Переименование API-полей ломает stores/views, даже если backend компилируется.
- Изменения editor graph model должны совпадать с `libraries/scenario` и validation в `scenario-repository`.
- Auth/session interceptors влияют на refresh/logout и могут проявляться только после истечения access token.
- `swagger.json` и `src/types/api` нужно держать согласованными с gateway/contracts.
