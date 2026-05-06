namespace MazyPlatform.Scenario.Abstractions.Nodes;

using System.Text.Json;

/// <summary>
/// Провайдер метаданных типа узла: тип, схема параметров, валидация.
/// Не требует DI и доступен в облегчённых сценариях
/// (например, в сервисе хранилища сценариев — только каталог без исполнителя).
/// </summary>
/// <remarks>
/// Реализации обязаны быть stateless и регистрируются как singleton.
/// Один тип узла в сборке должен иметь ровно один <see cref="INodeSchemaProvider"/>.
/// </remarks>
public interface INodeSchemaProvider
{
    /// <summary>
    /// Строковый тип узла (например "send_message").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Схема параметров узла для редактора и валидации.
    /// </summary>
    IReadOnlyList<NodeParamSchema> Schema { get; }

    /// <summary>
    /// Валидирует параметры узла перед публикацией сценария.
    /// </summary>
    /// <param name="nodeId">Идентификатор узла.</param>
    /// <param name="parameters">JSON-параметры узла.</param>
    /// <returns>Список ошибок валидации.</returns>
    IReadOnlyList<string> Validate(Guid nodeId, JsonElement parameters);
}
