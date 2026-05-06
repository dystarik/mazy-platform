namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class VerifyCodeHandler(
    IMfaSessionRepository mfaSessionRepository,
    VerifyMfaFactorService mfaFactorService,
    IUnitOfWork unitOfWork,
    ILogger<VerifyCodeHandler> logger) : ICommandHandler<VerifyCodeCommand, VerifyCodeResult>
{
    public async Task<Result<VerifyCodeResult>> HandleAsync(VerifyCodeCommand command, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(command.MfaSessionId);
        var userAccountId = Guid.Parse(command.UserAccountId);

        var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(mfaSessionId, userAccountId, cancellationToken);
        if (mfaSession is null)
        {
            MfaSessionNotFound(mfaSessionId, userAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительная или истёкшая MFA сессия.");
        }

        var verifyMfaFactorR = await mfaFactorService.ExecuteAsync(mfaSession, command.MfaMethodType, command.Code, cancellationToken);
        if (verifyMfaFactorR.IsFailure)
        {
            VerifyMfaFactorFailed(mfaSessionId, verifyMfaFactorR.Errors);
            return verifyMfaFactorR.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        VerifyMfaFactorSucceeded(mfaSessionId, verifyMfaFactorR.Value.IsCompleted);

        return new VerifyCodeResult(verifyMfaFactorR.Value.IsCompleted);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "MFA сессия с ID '{MfaSessionId}' для пользователя '{UserAccountId}' не найдена.")]
    private partial void MfaSessionNotFound(Guid mfaSessionId, Guid userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка верификации MFA фактора для сессии '{MfaSessionId}': {Errors}")]
    private partial void VerifyMfaFactorFailed(Guid mfaSessionId, object errors);

    [LoggerMessage(3, LogLevel.Information, "MFA фактор для сессии '{MfaSessionId}' успешно верифицирован. IsCompleted: {IsCompleted}")]
    private partial void VerifyMfaFactorSucceeded(Guid mfaSessionId, bool isCompleted);
    #endregion
}
