namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class SendUnauthenticatedEmailCodeHandler(
    IMfaSessionRepository mfaSessionRepository,
    IOneTimePasswordRepository otpRepository,
    ICodeHasher codeHasher,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<SendUnauthenticatedEmailCodeHandler> logger) : ICommandHandler<SendUnauthenticatedEmailCodeCommand>
{
    public async Task<Result> HandleAsync(SendUnauthenticatedEmailCodeCommand command, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(command.MfaSessionId);

        var mfaSession = await mfaSessionRepository.GetUnauthenticatedSessionByIdAsync(mfaSessionId, cancellationToken);
        if (mfaSession is null)
        {
            MfaUnauthenticatedSessionNotFound(mfaSessionId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительная или истёкшая MFA-сессия.");
        }

        var otp = OneTimePassword.Create(mfaSession.UserAccountId, OtpType.MfaEmail, codeHasher, timeProvider.GetUtcNow());

        otpRepository.Add(otp);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        EmailCodeSent(mfaSessionId, mfaSession.UserAccountId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "MFA сессия с ID '{MfaSessionId}' не найдена или не предназначена для неаутентифицированного сценария.")]
    private partial void MfaUnauthenticatedSessionNotFound(Guid mfaSessionId);

    [LoggerMessage(2, LogLevel.Information, "Email код успешно отправлен для неаутентифицированной MFA сессии '{MfaSessionId}' пользователя '{UserAccountId}'.")]
    private partial void EmailCodeSent(Guid mfaSessionId, Guid userAccountId);
    #endregion
}
