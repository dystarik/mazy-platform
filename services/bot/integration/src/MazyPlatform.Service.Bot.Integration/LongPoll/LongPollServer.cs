namespace MazyPlatform.Service.Bot.Integration.LongPoll;

/// <summary>
/// Данные Long Poll сервера VK.
/// </summary>
/// <param name="Server">URL сервера.</param>
/// <param name="Key">Ключ сессии.</param>
/// <param name="Ts">Номер последнего события.</param>
internal sealed record LongPollServer(string Server, string Key, string Ts);
