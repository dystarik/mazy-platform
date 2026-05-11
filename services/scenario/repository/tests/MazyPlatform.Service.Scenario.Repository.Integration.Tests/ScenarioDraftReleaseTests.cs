namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class ScenarioDraftReleaseTests : IntegrationTestBase
{
    [Test]
    public async Task UpdateAndValidateDraft_WithValidGraph_Should_Succeed()
    {
        var project = await App.Factory.CreateProjectWithValidDraftAsync();

        var draft = await App.ScenarioGraphs.GetScenarioDraftAsync(
            new GetScenarioDraftRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var validation = await App.ScenarioGraphs.ValidateScenarioDraftAsync(
            new ValidateScenarioDraftRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(draft.GraphJson).Contains("send_message");
        await Assert.That(validation.IsValid).IsTrue();
        await Assert.That(validation.Errors).IsEmpty();
    }

    [Test]
    public async Task ValidateScenarioDraft_WithInvalidGraph_Should_ReturnErrors()
    {
        var project = await App.Factory.CreateProjectAsync();
        await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest
            {
                ProjectId = project.ProjectId.ToString(),
                GraphJson = ScenarioRepositoryTestFactory.InvalidGraphJson(),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var validation = await App.ScenarioGraphs.ValidateScenarioDraftAsync(
            new ValidateScenarioDraftRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(validation.IsValid).IsFalse();
        await Assert.That(validation.Errors).IsNotEmpty();
    }

    [Test]
    public async Task PromoteToRelease_Should_CreateVersionOne_AndPublishReleaseChanged()
    {
        var project = await App.Factory.CreateProjectWithValidDraftAsync();
        var before = App.Events.Count;

        await App.Factory.PromoteDraftAsync(project);

        var released = await App.ScenarioGraphs.GetReleasedScenarioAsync(
            new GetReleasedScenarioRequest { ProjectId = project.ProjectId.ToString() },
            deadline: GrpcTestCall.Deadline);
        var version = await App.ScenarioGraphs.GetScenarioByVersionAsync(
            new GetScenarioByVersionRequest { ProjectId = project.ProjectId.ToString(), Version = 1 },
            deadline: GrpcTestCall.Deadline);
        var history = await App.ScenarioGraphs.GetVersionHistoryAsync(
            new GetVersionHistoryRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var releaseChanged = await App.Events.WaitForProjectAsync(RabbitMqEventCapture.ReleaseChanged, project.ProjectId, before);

        await Assert.That(released.Version).IsEqualTo(1);
        await Assert.That(version.Version).IsEqualTo(1);
        await Assert.That(version.GraphJson).IsEqualTo(released.GraphJson);
        await Assert.That(history.Versions.Any(v => v.Version == 1 && v.IsCurrent)).IsTrue();
        await Assert.That(releaseChanged.CurrentVersion).IsEqualTo(1);
    }

    [Test]
    public async Task SecondPromoteAndRollback_Should_CreateVersionTwo_ThenMakeVersionOneCurrent()
    {
        var project = await App.Factory.CreateProjectWithValidDraftAsync(text: "v1");
        await App.Factory.PromoteDraftAsync(project);
        await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest
            {
                ProjectId = project.ProjectId.ToString(),
                GraphJson = ScenarioRepositoryTestFactory.ValidGraphJson("v2"),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        await App.Factory.PromoteDraftAsync(project);

        await App.ScenarioGraphs.RollbackReleaseAsync(
            new RollbackReleaseRequest { ProjectId = project.ProjectId.ToString(), TargetVersion = 1 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        await App.ScenarioGraphs.RollbackReleaseAsync(
            new RollbackReleaseRequest { ProjectId = project.ProjectId.ToString(), TargetVersion = 1 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var released = await App.ScenarioGraphs.GetReleasedScenarioAsync(
            new GetReleasedScenarioRequest { ProjectId = project.ProjectId.ToString() },
            deadline: GrpcTestCall.Deadline);

        await Assert.That(released.Version).IsEqualTo(1);
    }

    [Test]
    public async Task DeleteScenarioVersion_NonCurrent_Should_PublishVersionDeleted()
    {
        var project = await App.Factory.CreateProjectWithValidDraftAsync(text: "v1");
        await App.Factory.PromoteDraftAsync(project);
        await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest
            {
                ProjectId = project.ProjectId.ToString(),
                GraphJson = ScenarioRepositoryTestFactory.ValidGraphJson("v2"),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        await App.Factory.PromoteDraftAsync(project);
        var before = App.Events.Count;

        await App.ScenarioGraphs.DeleteScenarioVersionAsync(
            new DeleteScenarioVersionRequest { ProjectId = project.ProjectId.ToString(), Version = 1 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var deleted = await App.Events.WaitForProjectAsync(RabbitMqEventCapture.VersionDeleted, project.ProjectId, before);

        await Assert.That(deleted.DeletedVersion).IsEqualTo(1);
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetScenarioByVersionAsync(
            new GetScenarioByVersionRequest { ProjectId = project.ProjectId.ToString(), Version = 1 },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task DeleteScenarioVersion_Current_Should_SwitchOrRemoveRelease_AndPublishEvents()
    {
        var project = await App.Factory.CreateProjectWithValidDraftAsync(text: "v1");
        await App.Factory.PromoteDraftAsync(project);
        await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest
            {
                ProjectId = project.ProjectId.ToString(),
                GraphJson = ScenarioRepositoryTestFactory.ValidGraphJson("v2"),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        await App.Factory.PromoteDraftAsync(project);

        await App.ScenarioGraphs.DeleteScenarioVersionAsync(
            new DeleteScenarioVersionRequest { ProjectId = project.ProjectId.ToString(), Version = 2 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var released = await App.ScenarioGraphs.GetReleasedScenarioAsync(
            new GetReleasedScenarioRequest { ProjectId = project.ProjectId.ToString() },
            deadline: GrpcTestCall.Deadline);
        var beforeRemoveLast = App.Events.Count;

        await App.ScenarioGraphs.DeleteScenarioVersionAsync(
            new DeleteScenarioVersionRequest { ProjectId = project.ProjectId.ToString(), Version = 1 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var removed = await App.Events.WaitForProjectAsync(RabbitMqEventCapture.ReleaseRemoved, project.ProjectId, beforeRemoveLast);

        await Assert.That(released.Version).IsEqualTo(1);
        await Assert.That(removed.ProjectId).IsEqualTo(project.ProjectId);
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetReleasedScenarioAsync(
            new GetReleasedScenarioRequest { ProjectId = project.ProjectId.ToString() },
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task ScenarioGraphFailPaths_Should_Fail()
    {
        var project = await App.Factory.CreateProjectAsync();
        var otherUser = ScenarioRepositoryTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.PromoteToReleaseAsync(
            new PromoteToReleaseRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest { ProjectId = project.ProjectId.ToString(), GraphJson = ScenarioRepositoryTestFactory.ValidGraphJson() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetScenarioDraftAsync(
            new GetScenarioDraftRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetVersionHistoryAsync(
            new GetVersionHistoryRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetReleasedScenarioAsync(
            new GetReleasedScenarioRequest { ProjectId = project.ProjectId.ToString() },
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest { ProjectId = "not-a-guid", GraphJson = ScenarioRepositoryTestFactory.ValidGraphJson() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.UpdateScenarioDraftAsync(
            new UpdateScenarioDraftRequest { ProjectId = project.ProjectId.ToString(), GraphJson = string.Empty },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task DeleteScenarioVersion_WhenBotsExist_Should_Fail()
    {
        var project = await App.Factory.CreateReleasedProjectAsync();
        App.BotManager.SetHasBots(project.ProjectId, 1, true);

        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.DeleteScenarioVersionAsync(
            new DeleteScenarioVersionRequest { ProjectId = project.ProjectId.ToString(), Version = 1 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }
}
