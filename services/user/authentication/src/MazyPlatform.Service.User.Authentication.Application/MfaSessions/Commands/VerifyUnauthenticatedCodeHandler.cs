namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class VerifyUnauthenticatedCodeHandler(
    IMfaSessionRepository mfaSessionRepository,
    VerifyMfaFactorService mfaFactorService,
    IUnitOfWork unitOfWork,
    ILogger<VerifyUnauthenticatedCodeHandler> logger) : ICommandHandler<VerifyUnauthenticatedCodeCommand, VerifyCodeResult>
{
    public async Task<Result<VerifyCodeResult>> HandleAsync(VerifyUnauthenticatedCodeCommand command, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(command.MfaSessionId);

        var mfaSession = await mfaSessionRepository.GetUnauthenticatedSessionByIdAsync(mfaSessionId, cancellationToken);
        if (mfaSession is null)
        {
            MfaUnauthenticatedSessionNotFound(mfaSessionId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительная или истёкшая MFA-сессия.");
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
    [LoggerMessage(1, LogLevel.Warning, "MFA сессия с ID '{MfaSessionId}' не найдена или не предназначена для неаутентифицированного сценария.")]
    private partial void MfaUnauthenticatedSessionNotFound(Guid mfaSessionId);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка верификации MFA фактора для неаутентифицированной сессии '{MfaSessionId}': {Errors}")]
    private partial void VerifyMfaFactorFailed(Guid mfaSessionId, object errors);

    [LoggerMessage(3, LogLevel.Information, "MFA фактор для неаутентифицированной сессии '{MfaSessionId}' успешно верифицирован. IsCompleted: {IsCompleted}")]
    private partial void VerifyMfaFactorSucceeded(Guid mfaSessionId, bool isCompleted);
    #endregion
}
