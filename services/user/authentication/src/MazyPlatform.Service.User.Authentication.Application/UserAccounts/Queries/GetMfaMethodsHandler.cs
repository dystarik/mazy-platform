namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

internal sealed partial class GetMfaMethodsHandler(
    IReadOnlyApplicationDbContext readDbContext,
    ILogger<GetMfaMethodsHandler> logger) : IQueryHandler<GetMfaMethodsQuery, GetMfaMethodsResult>
{
    public async Task<Result<GetMfaMethodsResult>> HandleAsync(GetMfaMethodsQuery query, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(query.UserAccountId);

        var mfaSettings = await readDbContext.MfaSettings.SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);
        if (mfaSettings is not null)
            return new GetMfaMethodsResult(mfaSettings.AvailableMfaMethodTypes);

        MfaSettingsNotFound(query.UserAccountId);
        return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный токен аутентификации.");
    }

    [LoggerMessage(1, LogLevel.Error, "Не найдена сущность MfaSettings для пользователя с ID '{UserAccountId}'")]
    private partial void MfaSettingsNotFound(string userAccountId);
}
