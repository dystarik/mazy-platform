namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class InternalValidateProjectForBotTests : IntegrationTestBase
{
    [Test]
    public async Task ValidateProjectForBot_WithReleasedUniversalProject_Should_AcceptTelegramAndVk()
    {
        var project = await App.Factory.CreateReleasedProjectAsync(platformType: PlatformType.Universal);

        await ValidateAsync(project, PlatformType.Telegram, 1);
        await ValidateAsync(project, PlatformType.Vk, 1);
    }

    [Test]
    public async Task ValidateProjectForBot_Should_RejectPlatformMismatch()
    {
        var telegramProject = await App.Factory.CreateReleasedProjectAsync(platformType: PlatformType.Telegram);
        var vkProject = await App.Factory.CreateReleasedProjectAsync(platformType: PlatformType.Vk);

        await GrpcAssert.ThrowsAsync(async () => await ValidateAsync(telegramProject, PlatformType.Vk, 1));
        await GrpcAssert.ThrowsAsync(async () => await ValidateAsync(vkProject, PlatformType.Telegram, 1));
    }

    [Test]
    public async Task ValidateProjectForBot_Should_RejectWrongOwnerAndMissingVersions()
    {
        var released = await App.Factory.CreateReleasedProjectAsync();
        var draftOnly = await App.Factory.CreateProjectWithValidDraftAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.Internal.ValidateProjectForBotAsync(
            new ValidateProjectForBotRequest
            {
                ProjectId = released.ProjectId.ToString(),
                OwnerAccountId = Guid.NewGuid().ToString(),
                PlatformType = PlatformType.Universal,
                ScenarioVersion = 1,
            },
            GrpcTestMetadata.ForInternal(ScenarioRepositoryFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await ValidateAsync(released, PlatformType.Universal, 999));
        await GrpcAssert.ThrowsAsync(async () => await ValidateAsync(draftOnly, PlatformType.Universal, 1));
        await GrpcAssert.ThrowsAsync(async () => await App.Internal.ValidateProjectForBotAsync(
            new ValidateProjectForBotRequest
            {
                ProjectId = Guid.NewGuid().ToString(),
                OwnerAccountId = released.OwnerAccountId.ToString(),
                PlatformType = PlatformType.Universal,
                ScenarioVersion = 1,
            },
            GrpcTestMetadata.ForInternal(ScenarioRepositoryFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline));
    }

    private static async Task ValidateAsync(TestProject project, PlatformType platformType, int scenarioVersion)
    {
        await App.Internal.ValidateProjectForBotAsync(
            new ValidateProjectForBotRequest
            {
                ProjectId = project.ProjectId.ToString(),
                OwnerAccountId = project.OwnerAccountId.ToString(),
                PlatformType = platformType,
                ScenarioVersion = scenarioVersion,
            },
            GrpcTestMetadata.ForInternal(ScenarioRepositoryFixture.InternalAccessToken),
            deadline: GrpcTestCall.Deadline);
    }
}
