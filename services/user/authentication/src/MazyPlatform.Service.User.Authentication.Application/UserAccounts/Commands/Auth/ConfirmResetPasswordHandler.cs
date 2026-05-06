namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class ConfirmResetPasswordHandler(
    ConfirmResetPasswordService confirmResetPasswordService,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmResetPasswordHandler> logger) : ICommandHandler<ConfirmResetPasswordCommand>
{
    public async Task<Result> HandleAsync(ConfirmResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var passwordR = Password.Create(command.NewPassword);
        if (passwordR.IsFailure)
        {
            InvalidPassword(passwordR.Errors);
            return passwordR.Errors;
        }

        Guid? otpId = string.IsNullOrWhiteSpace(command.OtpId) ? null : Guid.Parse(command.OtpId);
        Guid? mfaSessionId = string.IsNullOrWhiteSpace(command.MfaSessionId) ? null : Guid.Parse(command.MfaSessionId);

        var confirmR = await confirmResetPasswordService.ExecuteAsync(
            passwordR,
            otpId,
            command.OtpCode,
            mfaSessionId,
            cancellationToken);

        if (confirmR.IsFailure)
        {
            ConfirmResetPasswordFailed(otpId, mfaSessionId, confirmR.Errors);
            return confirmR.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        PasswordResetConfirmed(otpId, mfaSessionId);

        return Result.Success();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось создать ValueObject Password: {Errors}")]
    private partial void InvalidPassword(object errors);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка подтверждения сброса пароля. OtpId: {OtpId}, MfaSessionId: {MfaSessionId}, Errors: {Errors}")]
    private partial void ConfirmResetPasswordFailed(Guid? otpId, Guid? mfaSessionId, object errors);

    [LoggerMessage(3, LogLevel.Information, "Сброс пароля успешно подтвержден. OtpId: {OtpId}, MfaSessionId: {MfaSessionId}")]
    private partial void PasswordResetConfirmed(Guid? otpId, Guid? mfaSessionId);
    #endregion
}
