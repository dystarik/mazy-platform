namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class SetPasswordHandler(
    SetPasswordService setPasswordService,
    IMfaSessionRepository mfaSessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<SetPasswordHandler> logger) : ICommandHandler<SetPasswordCommand, SetPasswordResult>
{
    public async Task<Result<SetPasswordResult>> HandleAsync(SetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);

        var newPasswordR = Password.Create(command.NewPassword);
        if (newPasswordR.IsFailure)
        {
            InvalidPassword(newPasswordR.Errors);
            return newPasswordR.Errors;
        }

        Guid? mfaSessionId = string.IsNullOrWhiteSpace(command.MfaSessionId) ? null : Guid.Parse(command.MfaSessionId);
        var setPasswordR = await setPasswordService.ExecuteAsync(userAccountId, newPasswordR, mfaSessionId, cancellationToken);
        if (setPasswordR.IsFailure)
        {
            SetPasswordFailed(userAccountId, setPasswordR.Errors);
            return setPasswordR.Errors;
        }

        var result = setPasswordR.Value.RequiresMfa switch
        {
            true => HandleMfaRequired(setPasswordR.Value.MfaRequiredData),
            false => HandleSuccess(userAccountId),
        };

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private SetPasswordResult HandleMfaRequired(SetPasswordOutput.MfaRequired mfaRequiredData)
    {
        mfaSessionRepository.Add(mfaRequiredData.MfaSession);
        MfaChallengeIssued(mfaRequiredData.MfaSession.UserAccountId, mfaRequiredData.MfaSession.Id);
        return new SetPasswordResult(mfaRequiredData.MfaSession.Id, mfaRequiredData.MfaSession.RequiredFactorCount, mfaRequiredData.AvailableFactors);
    }

    private SetPasswordResult HandleSuccess(Guid userAccountId)
    {
        PasswordSet(userAccountId);
        return new SetPasswordResult();
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Не удалось создать ValueObject Password: {Errors}")]
    private partial void InvalidPassword(object errors);

    [LoggerMessage(2, LogLevel.Warning, "Ошибка установки пароля для пользователя '{UserAccountId}': {Errors}")]
    private partial void SetPasswordFailed(Guid userAccountId, object errors);

    [LoggerMessage(3, LogLevel.Information, "MFA-проверка инициирована для установки пароля пользователя '{UserAccountId}', сессия '{MfaSessionId}'.")]
    private partial void MfaChallengeIssued(Guid userAccountId, Guid mfaSessionId);

    [LoggerMessage(4, LogLevel.Information, "Пользователь '{UserAccountId}' успешно установил локальный пароль.")]
    private partial void PasswordSet(Guid userAccountId);
    #endregion
}
