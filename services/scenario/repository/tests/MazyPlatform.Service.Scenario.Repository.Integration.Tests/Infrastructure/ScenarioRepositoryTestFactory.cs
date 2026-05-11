namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using Grpc.Core;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

public sealed class ScenarioRepositoryTestFactory(ScenarioRepositoryFixture app)
{
    public static Guid CreateUserId() => Guid.NewGuid();

    public static string UniqueProjectName() => $"project-{Guid.NewGuid():N}";

    public static string UniqueSchemaName() => $"schema-{Guid.NewGuid():N}";

    public static string InvalidGraphJson() => "{}";

    public static string ValidGraphJson(string text = "Hello")
    {
        var startNodeId = Guid.NewGuid();
        var secondNodeId = Guid.NewGuid();

        return $$"""
            {
              "startNodeId": "{{startNodeId}}",
              "nodes": [
                {
                  "id": "{{startNodeId}}",
                  "type": "send_message",
                  "params": { "text": "{{text}}" }
                },
                {
                  "id": "{{secondNodeId}}",
                  "type": "receive_button_press",
                  "params": {
                    "buttonPayloadVariable": "payload",
                    "expectedPayloads": ["menu"],
                    "isEntry": true
                  }
                }
              ],
              "connections": [
                { "from": "{{startNodeId}}", "to": "{{secondNodeId}}" }
              ]
            }
            """;
    }

    public async Task<TestProject> CreateProjectAsync(
        Guid? ownerAccountId = null,
        PlatformType platformType = PlatformType.Universal)
    {
        var owner = ownerAccountId ?? CreateUserId();
        var name = UniqueProjectName();

        await app.EventPublisher.PublishEmailConfirmedAsync(owner);

        RpcException? lastException = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                var response = await app.Projects.CreateProjectAsync(
                    new CreateProjectRequest
                    {
                        Name = name,
                        PlatformType = platformType,
                    },
                    GrpcTestMetadata.ForUser(owner),
                    deadline: GrpcTestCall.Deadline);

                return new TestProject(owner, Guid.Parse(response.ProjectId), platformType, name);
            }
            catch (RpcException ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }

        if (lastException is not null)
        {
            var logs = await app.GetRepositoryLogsAsync();
            throw new InvalidOperationException($"CreateProject failed: {lastException.Status}. Logs: {logs}", lastException);
        }

        throw new InvalidOperationException("CreateProject failed without an exception.");
    }

    public async Task<TestProject> CreateProjectWithoutUserEventAsync(
        Guid ownerAccountId,
        PlatformType platformType = PlatformType.Universal)
    {
        var name = UniqueProjectName();
        var response = await app.Projects.CreateProjectAsync(
                new CreateProjectRequest
                {
                    Name = name,
                    PlatformType = platformType,
                },
                GrpcTestMetadata.ForUser(ownerAccountId),
                deadline: GrpcTestCall.Deadline);

        return new TestProject(ownerAccountId, Guid.Parse(response.ProjectId), platformType, name);
    }

    public Task<TestProject> CreateTelegramProjectAsync(Guid? ownerAccountId = null) =>
        CreateProjectAsync(ownerAccountId, PlatformType.Telegram);

    public Task<TestProject> CreateUniversalProjectAsync(Guid? ownerAccountId = null) =>
        CreateProjectAsync(ownerAccountId, PlatformType.Universal);

    public Task<TestProject> CreateVkProjectAsync(Guid? ownerAccountId = null) =>
        CreateProjectAsync(ownerAccountId, PlatformType.Vk);

    public async Task<TestProject> CreateProjectWithValidDraftAsync(
        Guid? ownerAccountId = null,
        PlatformType platformType = PlatformType.Universal,
        string text = "Hello")
    {
        var project = await CreateProjectAsync(ownerAccountId, platformType);
        await app.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest
            {
                ProjectId = project.ProjectId.ToString(),
                GraphJson = ValidGraphJson(text),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        return project;
    }

    public async Task PromoteDraftAsync(TestProject project)
    {
        await app.ScenarioGraphs.PromoteToReleaseAsync(
            new PromoteToReleaseRequest
            {
                ProjectId = project.ProjectId.ToString(),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
    }

    public async Task<TestProject> CreateReleasedProjectAsync(
        Guid? ownerAccountId = null,
        PlatformType platformType = PlatformType.Universal)
    {
        var project = await CreateProjectWithValidDraftAsync(ownerAccountId, platformType);
        await PromoteDraftAsync(project);
        return project;
    }
}
