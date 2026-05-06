namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Queries;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

internal sealed partial class GetStatusHandler(
    IReadOnlyApplicationDbContext readDbContext,
    TimeProvider timeProvider,
    ILogger<GetStatusHandler> logger) : IQueryHandler<GetStatusQuery, GetStatusResult>
{
    public async Task<Result<GetStatusResult>> HandleAsync(GetStatusQuery query, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(query.MfaSessionId);
        var userAccountId = Guid.Parse(query.UserAccountId);

        var mfaSession = await readDbContext.MfaSessions.SingleOrDefaultAsync(x => x.Id == mfaSessionId && x.UserAccountId == userAccountId, cancellationToken);
        if (mfaSession is null)
        {
            MfaSessionNotFound(mfaSessionId, userAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительная или истёкшая MFA сессия.");
        }

        var mfaSettings = await readDbContext.MfaSettings.SingleAsync(x => x.UserAccountId == userAccountId, cancellationToken: cancellationToken);

        return new GetStatusResult(
            mfaSession.IsCompleted,
            mfaSession.ExpiresAt <= timeProvider.GetUtcNow(),
            mfaSettings.AvailableMfaMethodTypes,
            mfaSession.CompletedFactors,
            mfaSession.RequiredFactorCount);
    }

    [LoggerMessage(1, LogLevel.Warning, "MFA сессия с ID '{MfaSessionId}' для пользователя '{UserAccountId}' не найдена.")]
    private partial void MfaSessionNotFound(Guid mfaSessionId, Guid userAccountId);
}
