namespace MazyPlatform.Service.Scenario.Repository.Application.UserData.Queries;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

internal sealed partial class GetUserDataRecordsHandler(
    ILogger<GetUserDataRecordsHandler> logger,
    IReadOnlyApplicationDbContext dbContext,
    IRuntimeUserDataReader runtimeUserDataReader) : IQueryHandler<GetUserDataRecordsQuery, GetUserDataRecordsResult>
{
    public async Task<Result<GetUserDataRecordsResult>> HandleAsync(
        GetUserDataRecordsQuery query,
        CancellationToken cancellationToken = default)
    {
        var projectId = Guid.Parse(query.ProjectId);
        var ownerAccountId = Guid.Parse(query.OwnerAccountId);

        var project = await dbContext.Projects
            .SingleOrDefaultAsync(x => x.Id == projectId, cancellationToken);

        if (project is null)
        {
            ProjectNotFound(projectId);
            return Error.NotFound(ErrorCodes.Project.NotFound, $"Проект '{projectId}' не найден.");
        }

        if (project.OwnerAccountId != ownerAccountId)
        {
            return Error.Unauthorized(ErrorCodes.Project.AccessDenied, $"Нет доступа к проекту '{projectId}'.");
        }

        var request = new RuntimeUserDataReadRequest(
            projectId,
            ParseOptionalGuid(query.SchemaId),
            query.ScenarioVersion,
            ParseOptionalGuid(query.BotId),
            string.IsNullOrWhiteSpace(query.PlatformUserId) ? null : query.PlatformUserId,
            query.IncludeArchived,
            query.PageSize == 0 ? 50 : query.PageSize,
            query.PageOffset);

        var records = await runtimeUserDataReader.GetRecordsAsync(request, cancellationToken);

        return new GetUserDataRecordsResult(
            records.Items.Select(ToResultItem).ToList(),
            records.TotalCount);
    }

    private static Guid? ParseOptionalGuid(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
    }

    private static GetUserDataRecordsResult.UserDataRecordItem ToResultItem(RuntimeUserDataRecord record)
    {
        return new GetUserDataRecordsResult.UserDataRecordItem(
            record.RecordId,
            record.SchemaId,
            record.SchemaSnapshotId,
            record.SchemaName,
            record.ScenarioVersion,
            record.BotId,
            record.PlatformUserId,
            record.SessionId,
            record.IsArchived,
            record.DataJson,
            record.CreatedAt,
            record.UpdatedAt);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Проект '{ProjectId}' не найден.")]
    private partial void ProjectNotFound(Guid projectId);
    #endregion
}
