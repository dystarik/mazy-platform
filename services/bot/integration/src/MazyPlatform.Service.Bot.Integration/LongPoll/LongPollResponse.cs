namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using System.Text.Json;

/// <summary>
/// Ответ Long Poll сервера VK.
/// </summary>
/// <param name="Ts">Новый номер события для следующего запроса.</param>
/// <param name="Updates">Список событий.</param>
/// <param name="Failed">Код ошибки (1 — устарел ts, 2 — истёк ключ, 3 — потеряна информация).</param>
internal sealed record LongPollResponse(string? Ts, IReadOnlyList<JsonElement> Updates, int? Failed);
