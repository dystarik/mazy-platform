namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using Google.Protobuf.WellKnownTypes;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class ProjectTests : IntegrationTestBase
{
    [Test]
    public async Task CreateProject_Should_ReturnProjectId_AndCreateEmptyDraftGraph()
    {
        var project = await App.Factory.CreateProjectAsync();

        var draft = await App.ScenarioGraphs.GetScenarioDraftAsync(
            new GetScenarioDraftRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(project.ProjectId).IsNotEqualTo(Guid.Empty);
        await Assert.That(draft.GraphJson).IsEqualTo("{}");
        await Assert.That(draft.Version).IsEqualTo(0);
    }

    [Test]
    public async Task GetProject_Should_ReturnCreatedProject()
    {
        var project = await App.Factory.CreateVkProjectAsync();

        var response = await App.Projects.GetProjectAsync(
            new GetProjectRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.ProjectId).IsEqualTo(project.ProjectId.ToString());
        await Assert.That(response.Name).IsEqualTo(project.Name);
        await Assert.That(response.PlatformType).IsEqualTo(PlatformType.Vk);
    }

    [Test]
    public async Task GetProjectList_Should_ReturnOnlyCurrentOwnerProjects()
    {
        var owner = ScenarioRepositoryTestFactory.CreateUserId();
        var ownProject = await App.Factory.CreateProjectAsync(owner);
        _ = await App.Factory.CreateProjectAsync();

        var response = await App.Projects.GetProjectListAsync(
            new Empty(),
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(response.Items.Any(i => string.Equals(i.ProjectId, ownProject.ProjectId.ToString(), StringComparison.Ordinal))).IsTrue();
        await Assert.That(response.Items.All(i => string.Equals(i.ProjectId, ownProject.ProjectId.ToString(), StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task OtherUser_Should_NotAccessOrDeleteProject()
    {
        var project = await App.Factory.CreateProjectAsync();
        var otherUser = ScenarioRepositoryTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.Projects.GetProjectAsync(
            new GetProjectRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Projects.DeleteProjectAsync(
            new DeleteProjectRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task DeleteProject_Should_DeleteGraph_AndPublishProjectDeleted()
    {
        var project = await App.Factory.CreateProjectAsync();
        var before = App.Events.Count;

        await App.Projects.DeleteProjectAsync(
            new DeleteProjectRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var deleted = await App.Events.WaitForProjectAsync(RabbitMqEventCapture.ProjectDeleted, project.ProjectId, before);

        await Assert.That(deleted.ProjectId).IsEqualTo(project.ProjectId);
        await GrpcAssert.ThrowsAsync(async () => await App.Projects.GetProjectAsync(
            new GetProjectRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.ScenarioGraphs.GetScenarioDraftAsync(
            new GetScenarioDraftRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task CreateProject_WithInvalidInput_Should_Fail()
    {
        var owner = ScenarioRepositoryTestFactory.CreateUserId();

        await GrpcAssert.ThrowsAsync(async () => await App.Projects.CreateProjectAsync(
            new CreateProjectRequest { Name = string.Empty, PlatformType = PlatformType.Universal },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Projects.CreateProjectAsync(
            new CreateProjectRequest { Name = ScenarioRepositoryTestFactory.UniqueProjectName(), PlatformType = PlatformType.Unspecified },
            GrpcTestMetadata.ForUser(owner),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task GetProject_WithMissingOrMalformedData_Should_Fail()
    {
        await GrpcAssert.ThrowsAsync(async () => await App.Projects.GetProjectAsync(
            new GetProjectRequest { ProjectId = Guid.NewGuid().ToString() },
            GrpcTestMetadata.ForUser(ScenarioRepositoryTestFactory.CreateUserId()),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Projects.GetProjectAsync(
            new GetProjectRequest { ProjectId = "not-a-guid" },
            GrpcTestMetadata.ForUser(ScenarioRepositoryTestFactory.CreateUserId()),
            deadline: GrpcTestCall.Deadline));

        await GrpcAssert.ThrowsAsync(async () => await App.Projects.GetProjectListAsync(new Empty()));
    }
}
