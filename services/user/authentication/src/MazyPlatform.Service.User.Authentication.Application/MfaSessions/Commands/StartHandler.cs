namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class StartHandler(
    IUserAccountRepository userAccountRepository,
    IMfaSessionRepository mfaSessionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<StartHandler> logger) : ICommandHandler<StartCommand, StartResult>
{
    public async Task<Result<StartResult>> HandleAsync(StartCommand command, CancellationToken cancellationToken = default)
    {
        var userAccountId = Guid.Parse(command.UserAccountId);
        var userAccount = await userAccountRepository.GetByIdAsync(userAccountId, cancellationToken);
        if (userAccount is null)
        {
            UserAccountNotFound(userAccountId);
            return Error.Internal(ErrorCodes.Auth.InternalError, "Внутренняя ошибка сервера.");
        }

        if (!userAccount.MfaSettings.HasConfirmedMfaMethod)
        {
            NoConfirmedMfaMethod(userAccountId);
            return Error.Validation(ErrorCodes.Auth.UserAccount.NoConfirmedMfaMethod, "Нет подтверждённого MFA-метода.");
        }

        var availableFactors = userAccount.MfaSettings.AvailableMfaMethodTypes;
        var mfaSession = MfaSession.Create(userAccount.Id, command.Action, availableFactors, timeProvider.GetUtcNow());
        mfaSessionRepository.Add(mfaSession);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        MfaSessionStarted(mfaSession.Id, userAccountId);
        return new StartResult(mfaSession.Id, mfaSession.RequiredFactorCount, availableFactors);
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Аккаунт пользователя '{UserAccountId}' не найден.")]
    private partial void UserAccountNotFound(Guid userAccountId);

    [LoggerMessage(2, LogLevel.Warning, "У пользователя '{UserAccountId}' нет подтвержденного MFA-метода.")]
    private partial void NoConfirmedMfaMethod(Guid userAccountId);

    [LoggerMessage(3, LogLevel.Information, "MFA сессия '{MfaSessionId}' успешно создана для пользователя '{UserAccountId}'.")]
    private partial void MfaSessionStarted(Guid mfaSessionId, Guid userAccountId);
    #endregion
}
