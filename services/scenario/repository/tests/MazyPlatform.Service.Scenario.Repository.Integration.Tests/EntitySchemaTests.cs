namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;
using MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

public sealed class EntitySchemaTests : IntegrationTestBase
{
    [Test]
    public async Task EntitySchemaLifecycle_Should_CreateUpdateListAndDelete()
    {
        var project = await App.Factory.CreateProjectAsync();
        var create = await App.EntitySchemas.CreateEntitySchemaAsync(
            new CreateEntitySchemaRequest
            {
                ProjectId = project.ProjectId.ToString(),
                Name = ScenarioRepositoryTestFactory.UniqueSchemaName(),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await App.EntitySchemas.UpdateEntitySchemaAsync(
            new UpdateEntitySchemaRequest
            {
                SchemaId = create.SchemaId,
                Name = "name",
                FieldType = FieldType.String,
                IsRequired = true,
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        await App.EntitySchemas.UpdateEntitySchemaAsync(
            new UpdateEntitySchemaRequest
            {
                SchemaId = create.SchemaId,
                Name = "score",
                FieldType = FieldType.Number,
                IsRequired = false,
                DefaultValue = "0",
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        var schema = await App.EntitySchemas.GetEntitySchemaAsync(
            new GetEntitySchemaRequest { SchemaId = create.SchemaId },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);
        var list = await App.EntitySchemas.GetEntitySchemaListAsync(
            new GetEntitySchemaListRequest { ProjectId = project.ProjectId.ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await Assert.That(schema.SchemaId).IsEqualTo(create.SchemaId);
        await Assert.That(schema.Fields.Any(f =>
            string.Equals(f.Name, "name", StringComparison.Ordinal) &&
            f.IsRequired &&
            f.FieldType == FieldType.String)).IsTrue();
        await Assert.That(schema.Fields.Any(f =>
            string.Equals(f.Name, "score", StringComparison.Ordinal) &&
            !f.IsRequired &&
            string.Equals(f.DefaultValue, "0", StringComparison.Ordinal))).IsTrue();
        await Assert.That(list.Items.Any(i => string.Equals(i.SchemaId, create.SchemaId, StringComparison.Ordinal))).IsTrue();

        await App.EntitySchemas.DeleteEntitySchemaAsync(
            new DeleteEntitySchemaRequest { SchemaId = create.SchemaId },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.GetEntitySchemaAsync(
            new GetEntitySchemaRequest { SchemaId = create.SchemaId },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task OtherUser_Should_NotAccessEntitySchemas()
    {
        var project = await App.Factory.CreateProjectAsync();
        var otherUser = ScenarioRepositoryTestFactory.CreateUserId();
        var create = await App.EntitySchemas.CreateEntitySchemaAsync(
            new CreateEntitySchemaRequest
            {
                ProjectId = project.ProjectId.ToString(),
                Name = ScenarioRepositoryTestFactory.UniqueSchemaName(),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.CreateEntitySchemaAsync(
            new CreateEntitySchemaRequest { ProjectId = project.ProjectId.ToString(), Name = ScenarioRepositoryTestFactory.UniqueSchemaName() },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.GetEntitySchemaAsync(
            new GetEntitySchemaRequest { SchemaId = create.SchemaId },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.UpdateEntitySchemaAsync(
            new UpdateEntitySchemaRequest { SchemaId = create.SchemaId, Name = "x", FieldType = FieldType.String },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.DeleteEntitySchemaAsync(
            new DeleteEntitySchemaRequest { SchemaId = create.SchemaId },
            GrpcTestMetadata.ForUser(otherUser),
            deadline: GrpcTestCall.Deadline));
    }

    [Test]
    public async Task EntitySchemaFailPaths_Should_Fail()
    {
        var project = await App.Factory.CreateProjectAsync();
        var create = await App.EntitySchemas.CreateEntitySchemaAsync(
            new CreateEntitySchemaRequest
            {
                ProjectId = project.ProjectId.ToString(),
                Name = ScenarioRepositoryTestFactory.UniqueSchemaName(),
            },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline);

        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.CreateEntitySchemaAsync(
            new CreateEntitySchemaRequest { ProjectId = project.ProjectId.ToString(), Name = string.Empty },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.UpdateEntitySchemaAsync(
            new UpdateEntitySchemaRequest { SchemaId = create.SchemaId, Name = string.Empty, FieldType = FieldType.String },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.UpdateEntitySchemaAsync(
            new UpdateEntitySchemaRequest { SchemaId = create.SchemaId, Name = "x", FieldType = FieldType.Unspecified },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.GetEntitySchemaAsync(
            new GetEntitySchemaRequest { SchemaId = "not-a-guid" },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
        await GrpcAssert.ThrowsAsync(async () => await App.EntitySchemas.DeleteEntitySchemaAsync(
            new DeleteEntitySchemaRequest { SchemaId = Guid.NewGuid().ToString() },
            GrpcTestMetadata.ForUser(project.OwnerAccountId),
            deadline: GrpcTestCall.Deadline));
    }
}
