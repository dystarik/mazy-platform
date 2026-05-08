# scenario

Библиотека runtime и модели сценариев. Она задает, какие nodes существуют, как они валидируются, как исполняются и как хранят runtime state.

## Используют

- `services/scenario/repository` - node catalog и validation draft/release.
- `services/scenario/engine` - исполнение graph по bot incoming events.
- tests библиотеки - проверка descriptors и runtime behavior.

## Основные модули

- `MazyPlatform.Scenario.Abstractions` - interfaces runtime, context, nodes.
- `MazyPlatform.Scenario` - core descriptors, validation и execution logic.
- `MazyPlatform.Scenario.Storage.Mongo` - Mongo storage.
- `Platforms/MazyPlatform.Scenario.Vk` - VK-specific nodes/actions.
- `Platforms/MazyPlatform.Scenario.Telegram` - Telegram-specific nodes/actions.

## Важно при изменениях

- Node type и параметры являются частью сохраненных draft/release graphs.
- Validation должна оставаться согласованной с frontend editor.
- Runtime changes должны учитывать уже опубликованные версии сценариев.
- Platform adapters не должны ломать общий runtime contract.
