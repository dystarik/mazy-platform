namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class LogoutAllHandler(
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    IUserSessionRepository userSessionRepository,
    ILogger<LogoutAllHandler> logger) : ICommandHandler<LogoutAllCommand>
{
    public async Task<Result> HandleAsync(LogoutAllCommand command, CancellationToken cancellationToken = default)
    {
        var accountId = Guid.Parse(command.UserAccountId);

        var sessions = await userSessionRepository.GetByUserAccountIdAsync(accountId, cancellationToken);
        if (sessions.Count is 0)
        {
            UserSessionsNotFound(accountId);
            return Result.Success();
        }

        var currentRefreshTokenId = Guid.Parse(command.CurrentRefreshTokenId);

        if (command.ExcludeCurrentSession)
            sessions = [.. sessions.Where(s => s.RefreshTokenId != currentRefreshTokenId)];

        foreach (var session in sessions)
            session.Revoke(RevokedReason.Logout, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (command.ExcludeCurrentSession)
            LogoutAllExcludingCurrent(accountId, sessions.Count);
        else
            LogoutAll(accountId, sessions.Count);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Активные сессии для пользователя {UserAccountId} не найдены, возможно уже завершены.")]
    private partial void UserSessionsNotFound(Guid userAccountId);

    [LoggerMessage(2, LogLevel.Information, "Все {SessionCount} сессии пользователя {UserAccountId} успешно завершены.")]
    private partial void LogoutAll(Guid userAccountId, int sessionCount);

    [LoggerMessage(3, LogLevel.Information, "Все сессии пользователя {UserAccountId} успешно завершены, кроме текущей. Завершено {SessionCount} сессий.")]
    private partial void LogoutAllExcludingCurrent(Guid userAccountId, int sessionCount);
    #endregion
}
