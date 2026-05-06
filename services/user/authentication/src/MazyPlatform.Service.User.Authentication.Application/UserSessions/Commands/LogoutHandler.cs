namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class LogoutHandler(
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    IUserSessionRepository userSessionRepository,
    ILogger<LogoutHandler> logger) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var refreshTokenId = Guid.Parse(command.RefreshTokenId);

        var session = await userSessionRepository.GetByRefreshTokenIdAsync(refreshTokenId, cancellationToken);
        if (session is null)
        {
            UserSessionNotFound(refreshTokenId);
            return Result.Success();
        }

        session.Revoke(RevokedReason.Logout, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
        LogoutSucceeded(session.Id, session.UserAccountId);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Information, "Сессия с RefreshTokenId '{RefreshTokenId}' не найдена, возможно уже завершена.")]
    private partial void UserSessionNotFound(Guid refreshTokenId);

    [LoggerMessage(2, LogLevel.Information, "Сессия {SessionId} пользователя {UserAccountId} успешно завершена.")]
    private partial void LogoutSucceeded(Guid sessionId, Guid userAccountId);
    #endregion
}
