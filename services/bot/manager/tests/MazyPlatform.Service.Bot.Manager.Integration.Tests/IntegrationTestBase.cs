namespace MazyPlatform.Service.Bot.Manager.Integration.Tests;

using MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

[NotInParallel("bot-manager")]
[Category("Integration")]
public abstract class IntegrationTestBase
{
    protected static BotManagerFixture App => BotManagerFixture.Shared;
}
