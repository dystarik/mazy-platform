namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class ResetPasswordHandler(
    ResetPasswordService resetPasswordService,
    IOneTimePasswordRepository otpRepository,
    IMfaSessionRepository mfaSessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ResetPasswordHandler> logger) : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
    public async Task<Result<ResetPasswordResult>> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var emailR = Email.Create(command.Email);
        if (emailR.IsFailure)
        {
            InvalidEmail(command.Email, emailR.Errors);
            return emailR.Errors;
        }

        var resetPasswordR = await resetPasswordService.ExecuteAsync(emailR, cancellationToken);
        if (resetPasswordR.IsFailure)
        {
            ResetPasswordFailed(command.Email, resetPasswordR.Errors);
            return resetPasswordR.Errors;
        }

        var result = resetPasswordR.Value.RequiresMfa switch
        {
            true => HandleMfaChallenge(resetPasswordR.Value.MfaChallengeData),
            false => HandleOtpChallenge(resetPasswordR.Value.OtpChallengeData),
        };

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private ResetPasswordResult HandleOtpChallenge(ResetPasswordOutput.OtpChallenge otpChallenge)
    {
        otpRepository.Add(otpChallenge.Otp);

        OtpChallengeIssued(otpChallenge.Otp.UserAccountId, otpChallenge.Otp.Id);
        return new ResetPasswordResult(otpChallenge.Otp.Id);
    }

    private ResetPasswordResult HandleMfaChallenge(ResetPasswordOutput.MfaChallenge mfaChallenge)
    {
        mfaSessionRepository.Add(mfaChallenge.MfaSession);

        MfaChallengeIssued(mfaChallenge.MfaSession.UserAccountId, mfaChallenge.MfaSession.Id);
        return new ResetPasswordResult(mfaChallenge.MfaSession.Id, mfaChallenge.MfaSession.RequiredFactorCount, mfaChallenge.AvailableFactors);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось создать ValueObject Email из строки '{Email}': {Errors}")]
    private partial void InvalidEmail(string email, object errors);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка выполнения доменного сервиса ResetPasswordService для пользователя с email '{Email}': {Errors}")]
    private partial void ResetPasswordFailed(string email, object errors);

    [LoggerMessage(3, LogLevel.Information, "Для пользователя {UserAccountId} выдан OTP на сброс пароля. OTP: {OtpId}")]
    private partial void OtpChallengeIssued(Guid userAccountId, Guid otpId);

    [LoggerMessage(4, LogLevel.Information, "Для пользователя {UserAccountId} требуется MFA-верификация для сброса пароля. Создана MFA сессия {MfaSessionId}.")]
    private partial void MfaChallengeIssued(Guid userAccountId, Guid mfaSessionId);
    #endregion
}
