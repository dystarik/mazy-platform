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

internal sealed partial class SendEmailCodeHandler(
    IMfaSessionRepository mfaSessionRepository,
    IOneTimePasswordRepository otpRepository,
    ICodeHasher codeHasher,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<SendEmailCodeHandler> logger) : ICommandHandler<SendEmailCodeCommand>
{
    public async Task<Result> HandleAsync(SendEmailCodeCommand command, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(command.MfaSessionId);
        var userAccountId = Guid.Parse(command.UserAccountId);

        var mfaSession = await mfaSessionRepository.GetByIdAndUserAccountIdAsync(mfaSessionId, userAccountId, cancellationToken);
        if (mfaSession is null)
        {
            MfaSessionNotFound(mfaSessionId, userAccountId);
            return Error.Unauthorized(ErrorCodes.Auth.Unauthorized, "Недействительная или истёкшая MFA сессия.");
        }

        var otp = OneTimePassword.Create(userAccountId, OtpType.MfaEmail, codeHasher, timeProvider.GetUtcNow());

        otpRepository.Add(otp);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        EmailCodeSent(mfaSessionId, userAccountId);
        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "MFA сессия с ID '{MfaSessionId}' для пользователя '{UserAccountId}' не найдена.")]
    private partial void MfaSessionNotFound(Guid mfaSessionId, Guid userAccountId);

    [LoggerMessage(2, LogLevel.Information, "Email код успешно отправлен для MFA сессии '{MfaSessionId}' пользователя '{UserAccountId}'.")]
    private partial void EmailCodeSent(Guid mfaSessionId, Guid userAccountId);
    #endregion
}
