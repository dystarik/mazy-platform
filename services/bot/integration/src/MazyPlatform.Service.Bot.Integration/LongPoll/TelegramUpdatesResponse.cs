namespace MazyPlatform.Service.Bot.Integration.LongPoll;

using System.Text.Json;

internal sealed record TelegramUpdatesResponse(IReadOnlyList<JsonElement> Updates);
