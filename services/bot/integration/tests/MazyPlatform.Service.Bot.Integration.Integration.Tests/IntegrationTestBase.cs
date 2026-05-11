namespace MazyPlatform.Service.Bot.Integration.Integration.Tests;

using MazyPlatform.Service.Bot.Integration.Integration.Tests.Infrastructure;

[NotInParallel("bot-integration")]
[Category("Integration")]
public abstract class IntegrationTestBase
{
    protected static BotIntegrationFixture App => BotIntegrationFixture.Shared;
}
