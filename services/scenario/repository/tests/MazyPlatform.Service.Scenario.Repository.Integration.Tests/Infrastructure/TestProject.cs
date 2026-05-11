namespace MazyPlatform.Service.Scenario.Repository.Integration.Tests.Infrastructure;

using MazyPlatform.Contracts.Scenario.Repository.Grpc;

public sealed record TestProject(Guid OwnerAccountId, Guid ProjectId, PlatformType PlatformType, string Name);
