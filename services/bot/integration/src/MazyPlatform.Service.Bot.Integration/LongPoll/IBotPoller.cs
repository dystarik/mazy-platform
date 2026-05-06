namespace MazyPlatform.Service.Bot.Integration.LongPoll;

internal interface IBotPoller
{
    Task RunAsync(CancellationToken cancellationToken);
}
