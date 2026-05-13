# scenario

Библиотека модели, validation и runtime исполнения сценариев. Она определяет node catalog, параметры нод, graph validation, executor, platform adapters и Mongo storage runtime state.

## Где смотреть код

- `src/MazyPlatform.Scenario.Abstractions` - interfaces, graph, actions, data, sessions, incoming events.
- `src/MazyPlatform.Scenario` - builder, executor, node registry/catalog, validation, common nodes.
- `src/MazyPlatform.Scenario.Storage.Mongo` - session/schema/data stores и BSON serialization.
- `src/Platforms/MazyPlatform.Scenario.Vk` - VK-specific nodes/actions/adapters.
- `src/Platforms/MazyPlatform.Scenario.Telegram` - Telegram-specific nodes/actions/adapters.
- `tests/MazyPlatform.Scenario.Tests` - descriptors, validator, executor, storage-facing behavior helpers.

Используют `services/scenario/repository` для catalog/validation и `services/scenario/engine` для исполнения release graph.

## Как проверять

```powershell
dotnet test libraries\scenario\MazyPlatform.Scenario.slnx
```

После изменений runtime или descriptors дополнительно проверьте:

- `services/scenario/repository` integration tests для node catalog и draft/release validation;
- `services/scenario/engine` integration tests для execution/cache/delay resume;
- frontend editor: отображение node params, сохранение draft, validate.

## Частые места изменений

- Новая node: descriptor + implementation + registration/scanner expectations + tests.
- Новый параметр node: schema/default/validation + frontend editor compatibility.
- Новое outgoing action или incoming event: abstractions + platform adapter + scenario-engine handling.
- Изменения Mongo storage: serialization, indexes, backward compatibility existing documents.

## Рискованные изменения

- `Node type`, parameter names/types и graph shape уже сохранены в draft/release graphs.
- Runtime executor должен уметь исполнять опубликованные версии, созданные до изменения.
- Validation в библиотеке, scenario-repository и frontend editor должна говорить об одном и том же.
- Delay/session state хранится в MongoDB; изменение формата требует миграционной стратегии.
- Platform adapters не должны протаскивать platform-specific детали в общие abstractions без необходимости.
