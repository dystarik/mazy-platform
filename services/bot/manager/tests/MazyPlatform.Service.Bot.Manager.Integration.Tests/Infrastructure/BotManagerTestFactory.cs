namespace MazyPlatform.Service.Bot.Manager.Integration.Tests.Infrastructure;

using Grpc.Core;

using MazyPlatform.Contracts.Bot.Grpc.Manager;

using ScenarioPlatformType = MazyPlatform.Contracts.Scenario.Repository.Grpc.PlatformType;

public sealed class BotManagerTestFactory(BotManagerFixture app)
{
    public static Guid CreateUserId() => Guid.NewGuid();

    public static string UniqueBotName() => $"bot-{Guid.NewGuid():N}";

    public static string UniqueTelegramToken() => $"123456:{Guid.NewGuid():N}";

    public static string UniqueVkToken() => $"vk1.a.{Guid.NewGuid():N}";

    public async Task<Guid> CreateUserAsync(Guid? userAccountId = null)
    {
        var user = userAccountId ?? CreateUserId();
        await app.EventPublisher.PublishEmailConfirmedAsync(user);
        return user;
    }

    public void RegisterValidScenarioProject(
        Guid ownerAccountId,
        Guid projectId,
        ScenarioPlatformType platformType,
        params int[] versions)
    {
        app.ScenarioRepository.RegisterProject(ownerAccountId, projectId, platformType, versions);
    }

    public async Task<TestBot> CreateTelegramBotWithoutProjectAsync(Guid? ownerAccountId = null)
    {
        var owner = await CreateUserAsync(ownerAccountId);
        var name = UniqueBotName();
        var token = UniqueTelegramToken();

        var response = await RetryBotCreateAsync(async () => await app.Bots.CreateBotWithoutProjectAsync(
            new CreateBotWithoutProjectRequest
            {
                Name = name,
                PlatformType = BotPlatformType.Telegram,
                AccessToken = token,
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        return new TestBot(Guid.Parse(response.BotInstanceId), owner, null, name, BotPlatformType.Telegram, token, null, null);
    }

    public async Task<TestBot> CreateVkBotWithoutProjectAsync(Guid? ownerAccountId = null)
    {
        var owner = await CreateUserAsync(ownerAccountId);
        var name = UniqueBotName();
        var token = UniqueVkToken();
        var communityId = Guid.NewGuid().ToString("N");

        var response = await RetryBotCreateAsync(async () => await app.Bots.CreateBotWithoutProjectAsync(
            new CreateBotWithoutProjectRequest
            {
                Name = name,
                PlatformType = BotPlatformType.Vk,
                AccessToken = token,
                CommunityId = communityId,
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        return new TestBot(Guid.Parse(response.BotInstanceId), owner, null, name, BotPlatformType.Vk, token, communityId, null);
    }

    public async Task<TestBot> CreateTelegramBotAsync(
        Guid? ownerAccountId = null,
        Guid? projectId = null,
        int scenarioVersion = 1,
        BotScenarioVersionUpdateMode updateMode = BotScenarioVersionUpdateMode.Auto)
    {
        var owner = await CreateUserAsync(ownerAccountId);
        var project = projectId ?? Guid.NewGuid();
        RegisterValidScenarioProject(owner, project, ScenarioPlatformType.Telegram, scenarioVersion, scenarioVersion + 1);
        var name = UniqueBotName();
        var token = UniqueTelegramToken();

        var request = new CreateBotRequest
        {
            ProjectId = project.ToString(),
            Name = name,
            PlatformType = BotPlatformType.Telegram,
            AccessToken = token,
            ScenarioVersion = scenarioVersion,
            ScenarioVersionUpdateMode = updateMode,
        };

        var response = await RetryBotCreateAsync(async () => await app.Bots.CreateBotAsync(
            request,
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        return new TestBot(Guid.Parse(response.BotInstanceId), owner, project, name, BotPlatformType.Telegram, token, null, scenarioVersion);
    }

    public async Task<TestBot> CreateVkBotAsync(
        Guid? ownerAccountId = null,
        Guid? projectId = null,
        int scenarioVersion = 1,
        BotScenarioVersionUpdateMode updateMode = BotScenarioVersionUpdateMode.Auto)
    {
        var owner = await CreateUserAsync(ownerAccountId);
        var project = projectId ?? Guid.NewGuid();
        RegisterValidScenarioProject(owner, project, ScenarioPlatformType.Vk, scenarioVersion, scenarioVersion + 1);
        var name = UniqueBotName();
        var token = UniqueVkToken();
        var communityId = Guid.NewGuid().ToString("N");

        var response = await RetryBotCreateAsync(async () => await app.Bots.CreateBotAsync(
            new CreateBotRequest
            {
                ProjectId = project.ToString(),
                Name = name,
                PlatformType = BotPlatformType.Vk,
                AccessToken = token,
                CommunityId = communityId,
                ScenarioVersion = scenarioVersion,
                ScenarioVersionUpdateMode = updateMode,
            },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        return new TestBot(Guid.Parse(response.BotInstanceId), owner, project, name, BotPlatformType.Vk, token, communityId, scenarioVersion);
    }

    public async Task<TestBot> CreateActiveBotAsync(
        Guid? ownerAccountId = null,
        Guid? projectId = null,
        int scenarioVersion = 1,
        BotScenarioVersionUpdateMode updateMode = BotScenarioVersionUpdateMode.Auto)
    {
        var bot = await CreateTelegramBotAsync(ownerAccountId, projectId, scenarioVersion, updateMode);
        await app.Bots.ActivateBotAsync(
            new ActivateBotRequest { BotInstanceId = bot.BotInstanceId.ToString() },
            GrpcTestMetadata.ForUser(bot.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        return bot;
    }

    public async Task<(TestBot First, TestBot Second)> CreateTwoUsersWithBotsAsync()
    {
        var first = await CreateTelegramBotAsync();
        var second = await CreateTelegramBotAsync();
        return (first, second);
    }

    private async Task<CreateBotResponse> RetryBotCreateAsync(Func<Task<CreateBotResponse>> action)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                return await action();
            }
            catch (RpcException ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }

        var logs = await app.GetBotManagerLogsAsync();
        throw new InvalidOperationException($"Bot create did not succeed. Last exception: {lastException?.Message}. Logs: {logs}", lastException);
    }
}
