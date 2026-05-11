namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class UserDataTests : IntegrationTestBase
{
    [Test]
    public async Task GetUserDataRecords_Should_ReturnAndFilterSeededMongoRecords()
    {
        var project = await App.Factory.CreateProjectAsync();
        var schemaId = Guid.NewGuid();
        var otherSchemaId = Guid.NewGuid();
        var botId = Guid.NewGuid();
        var otherBotId = Guid.NewGuid();
        var seeder = new MongoRuntimeDataSeeder(App);

        await seeder.SeedRecordAsync(project.ProjectId, schemaId, Guid.NewGuid(), 1, botId, "platform-user-1", false, "first");
        await seeder.SeedRecordAsync(project.ProjectId, schemaId, Guid.NewGuid(), 2, otherBotId, "platform-user-2", true, "archived");
        await seeder.SeedRecordAsync(project.ProjectId, otherSchemaId, Guid.NewGuid(), 1, botId, "platform-user-3", false, "other-schema");

        var all = await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), PageSize = 20 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var bySchema = await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), SchemaId = schemaId.ToString(), PageSize = 20 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var includeArchived = await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), IncludeArchived = true, PageSize = 20 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var byBot = await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), BotId = botId.ToString(), PageSize = 20 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var byPlatformUser = await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), PlatformUserId = "platform-user-1", PageSize = 20 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(all.TotalCount).IsEqualTo(2);
        await Assert.That(bySchema.TotalCount).IsEqualTo(1);
        await Assert.That(includeArchived.TotalCount).IsEqualTo(3);
        await Assert.That(byBot.TotalCount).IsEqualTo(2);
        await Assert.That(byPlatformUser.TotalCount).IsEqualTo(1);
        await Assert.That(byPlatformUser.Items[0].DataJson).Contains("first");
    }

    [Test]
    public async Task GetUserDataRecords_PaginationAndAccessRules_Should_Work()
    {
        var project = await App.Factory.CreateProjectAsync();
        var seeder = new MongoRuntimeDataSeeder(App);

        await seeder.SeedRecordAsync(project.ProjectId, Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "u1", false, "one");
        await seeder.SeedRecordAsync(project.ProjectId, Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "u2", false, "two");

        var page = await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), PageSize = 1, PageOffset = 1 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(page.TotalCount).IsEqualTo(2);
        await Assert.That(page.Items.Count).IsEqualTo(1);
        await GrpcAssert.ThrowsAsync(async () => await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), PageSize = 20 },
            GrpcTestMetadata.ForUser(Guid.NewGuid()),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task GetUserDataRecords_WithMalformedFilters_Should_Fail()
    {
        var project = await App.Factory.CreateProjectAsync();

        await GrpcAssert.ThrowsAsync(async () => await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = "not-a-guid" },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), SchemaId = "not-a-guid" },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), BotId = "not-a-guid" },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.UserData.GetUserDataRecordsAsync(
            new GetUserDataRecordsRequest { ProjectId = project.ProjectId.ToString(), PageSize = 201 },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }
}
