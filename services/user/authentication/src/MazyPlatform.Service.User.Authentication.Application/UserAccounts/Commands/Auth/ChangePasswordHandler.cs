namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class ChangePasswordHandler(
    ChangePasswordService changePasswordService,
    IMfaSessionRepository mfaSessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ChangePasswordHandler> logger) : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<Result<ChangePasswordResult>> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);

        var currentPasswordR = Password.Create(command.CurrentPassword);
        if (currentPasswordR.IsFailure)
        {
            InvalidPassword(currentPasswordR.Errors);
            return currentPasswordR.Errors;
        }

        var newPasswordR = Password.Create(command.NewPassword);
        if (newPasswordR.IsFailure)
        {
            InvalidPassword(newPasswordR.Errors);
            return newPasswordR.Errors;
        }

        Guid? mfaSessionId = string.IsNullOrWhiteSpace(command.MfaSessionId) ? null : Guid.Parse(command.MfaSessionId);
        var changePasswordR = await changePasswordService.ExecuteAsync(userAccountId, currentPasswordR, newPasswordR, mfaSessionId, cancellationToken);
        if (changePasswordR.IsFailure)
        {
            ChangePasswordFailed(userAccountId, changePasswordR.Errors);
            return changePasswordR.Errors;
        }

        var result = changePasswordR.Value.RequiresMfa switch
        {
            true => HandleMfaRequired(changePasswordR.Value.MfaRequiredData),
            false => HandleSuccess(userAccountId),
        };

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private ChangePasswordResult HandleMfaRequired(ChangePasswordOutput.MfaRequired mfaRequiredData)
    {
        mfaSessionRepository.Add(mfaRequiredData.MfaSession);
        MfaChallengeIssued(mfaRequiredData.MfaSession.UserAccountId, mfaRequiredData.MfaSession.Id);
        return new ChangePasswordResult(mfaRequiredData.MfaSession.Id, mfaRequiredData.MfaSession.RequiredFactorCount, mfaRequiredData.AvailableFactors);
    }

    private ChangePasswordResult HandleSuccess(Guid userAccountId)
    {
        PasswordChanged(userAccountId);
        return new ChangePasswordResult();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось создать ValueObject Password: {Errors}")]
    private partial void InvalidPassword(object errors);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка смены пароля для пользователя '{UserAccountId}': {Errors}")]
    private partial void ChangePasswordFailed(Guid userAccountId, object errors);

    [LoggerMessage(3, LogLevel.Information, "MFA-проверка инициирована для пользователя '{UserAccountId}', сессия '{MfaSessionId}'.")]
    private partial void MfaChallengeIssued(Guid userAccountId, Guid mfaSessionId);

    [LoggerMessage(4, LogLevel.Information, "Пользователь '{UserAccountId}' успешно сменил пароль.")]
    private partial void PasswordChanged(Guid userAccountId);
    #endregion
}
