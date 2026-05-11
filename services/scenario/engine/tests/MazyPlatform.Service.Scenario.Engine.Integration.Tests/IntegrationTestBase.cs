namespace MazyPlatform.Service.Scenario.Engine.Integration.Tests;

using MazyPlatform.Service.Scenario.Engine.Integration.Tests.Infrastructure;

[NotInParallel("scenario-engine")]
[Category("Integration")]
public abstract class IntegrationTestBase
{
    protected static ScenarioEngineFixture App => ScenarioEngineFixture.Shared;
}
