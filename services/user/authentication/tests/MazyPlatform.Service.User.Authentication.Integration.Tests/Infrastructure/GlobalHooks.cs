namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using TUnit.Core;

public static class GlobalHooks
{
    [Before(Assembly)]
    public static async Task StartAuthServiceAsync()
    {
        await AuthServiceFixture.Shared.StartAsync();
    }

    [After(Assembly)]
    public static async Task StopAuthServiceAsync()
    {
        await AuthServiceFixture.Shared.DisposeAsync();
    }
}
