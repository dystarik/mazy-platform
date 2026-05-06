namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;

/// <summary>
/// Режим обновления версии сценария, назначенной боту.
/// </summary>
public enum ScenarioVersionUpdateMode
{
    /// <summary>Автоматически переходить на новую опубликованную версию сценария.</summary>
    Auto = 1,

    /// <summary>Оставаться на текущей версии до ручного переключения.</summary>
    Manual = 2,
}
