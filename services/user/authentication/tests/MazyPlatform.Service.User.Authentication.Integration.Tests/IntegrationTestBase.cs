namespace MazyPlatform.Service.User.Authentication.Integration.Tests;

using MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

[NotInParallel("auth-service")]
[Category("Integration")]
public abstract class IntegrationTestBase
{
    protected static AuthServiceFixture App => AuthServiceFixture.Shared;
}
