namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

[NotInParallel("scenario-repository")]
[Category("Integration")]
public abstract class IntegrationTestBase
{
    protected static ScenarioRepositoryFixture App => ScenarioRepositoryFixture.Shared;
}
