namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class RemoveMfaMethodHandler(
    IMfaSessionRepository mfaSessionRepository,
    IUserAccountRepository userAccountRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<RemoveMfaMethodHandler> logger) : ICommandHandler<RemoveMfaMethodCommand>
{
    public async Task<Result> HandleAsync(RemoveMfaMethodCommand command, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(command.MfaSessionId);
        var userAccountId = Guid.Parse(command.UserAccountId);
        var now = timeProvider.GetUtcNow();

        var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(mfaSessionId, userAccountId, cancellationToken);
        if (mfaSession is null)
        {
            MfaSessionNotFound(mfaSessionId, userAccountId);
            return Error.NotFound(ErrorCodes.Auth.MfaSession.NotFound, "MFA сессия не найдена.");
        }

        if (mfaSession.Action != MfaSessionAction.DeleteMfaMethod)
        {
            MfaSessionActionMismatch(mfaSessionId, mfaSession.Action);
            return Error.Validation(ErrorCodes.Auth.Validation.Invalid, "MFA сессия не предназначена для удаления метода.");
        }

        if (!mfaSession.IsCompleted || mfaSession.ValidUntil < now)
        {
            MfaSessionNotCompleted(mfaSessionId);
            return Error.Unauthorized(ErrorCodes.Auth.MfaSession.Expired, "Недействительная MFA-сессия.");
        }

        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(userAccountId);
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");
        }

        var removeR = userAccount.RemoveMfaMethod(command.MfaMethodType, now);
        if (removeR.IsFailure)
        {
            RemoveMfaMethodFailed(userAccount.Email.Value, command.MfaMethodType, removeR.Errors);
            return removeR.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        MfaMethodRemoved(userAccount.Email.Value, command.MfaMethodType);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "MFA сессия '{MfaSessionId}' для пользователя '{UserAccountId}' не найдена.")]
    private partial void MfaSessionNotFound(Guid mfaSessionId, Guid userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "MFA сессия '{MfaSessionId}' имеет неверное действие '{Action}' для удаления метода.")]
    private partial void MfaSessionActionMismatch(Guid mfaSessionId, MfaSessionAction action);

    [LoggerMessage(3, LogLevel.Warning, "MFA сессия '{MfaSessionId}' не завершена или истекла.")]
    private partial void MfaSessionNotCompleted(Guid mfaSessionId);

    [LoggerMessage(4, LogLevel.Critical, "Критическое нарушение целостности данных: UserAccount с ID '{UserAccountId}' не найден.")]
    private partial void UserAccountNotFound(Guid userAccountId);

    [LoggerMessage(5, LogLevel.Warning, "Ошибка удаления MFA-метода '{MfaMethodType}' для пользователя '{Email}': {Errors}")]
    private partial void RemoveMfaMethodFailed(string email, MfaMethodType mfaMethodType, object errors);

    [LoggerMessage(6, LogLevel.Information, "Пользователь '{Email}' успешно удалил MFA-метод '{MfaMethodType}'.")]
    private partial void MfaMethodRemoved(string email, MfaMethodType mfaMethodType);
    #endregion
}
