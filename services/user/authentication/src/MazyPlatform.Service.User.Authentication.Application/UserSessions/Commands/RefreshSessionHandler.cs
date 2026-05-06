namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class RefreshSessionHandler(
    IUserSessionRepository sessionRepository,
    IUserAccountRepository accountRepository,
    ITokenHasher tokenHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RefreshSessionHandler> logger) : ICommandHandler<RefreshSessionCommand, RefreshSessionResult>
{
    public async Task<Result<RefreshSessionResult>> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken = default)
    {
        var refreshTokenId = Guid.Parse(command.RefreshTokenId);
        var session = await sessionRepository.GetByRefreshTokenIdAsync(refreshTokenId, cancellationToken);
        if (session is null)
        {
            UserSessionNotFound(refreshTokenId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительный или просроченный токен обновления.");
        }

        var now = timeProvider.GetUtcNow();

        var refreshToken = Guid.NewGuid().ToString();
        var refreshTokenHash = RefreshTokenHash.FromTrusted(tokenHasher.Hash(refreshToken));

        var refreshSessionR = session.RefreshSession(tokenHasher, command.RefreshToken, refreshTokenHash, now);
        if (refreshSessionR.IsFailure)
        {
            RefreshSessionFailed(session.Id, session.UserAccountId, refreshSessionR.Errors);
            return refreshSessionR.Errors;
        }

        var account = await accountRepository.GetByIdAsync(session.UserAccountId, cancellationToken);
        if (account is null)
        {
            UserAccountNotFound(session.Id, session.UserAccountId);
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");
        }

        var accessToken = jwtTokenGenerator.Generate(session.UserAccountId, session.RefreshTokenId, account.Email);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        RefreshSessionSucceeded(session.Id, account.Email.Value);

        return new RefreshSessionResult(accessToken, refreshToken);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Сессия с RefreshTokenId '{RefreshTokenId}' не найдена, возможно отозвана или токен недействителен.")]
    private partial void UserSessionNotFound(Guid refreshTokenId);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка при обновлении сессии {SessionId} пользователя {UserAccountId}: {Errors}")]
    private partial void RefreshSessionFailed(Guid sessionId, Guid userAccountId, object errors);

    [LoggerMessage(3, LogLevel.Critical, "Критическое нарушение целостности данных: UserSession {SessionId} ссылается на несуществующий UserAccount {UserAccountId}")]
    private partial void UserAccountNotFound(Guid sessionId, Guid userAccountId);

    [LoggerMessage(4, LogLevel.Information, "Сессия {SessionId} пользователя с email '{Email}' успешно обновлена.")]
    private partial void RefreshSessionSucceeded(Guid sessionId, string email);
    #endregion
}
