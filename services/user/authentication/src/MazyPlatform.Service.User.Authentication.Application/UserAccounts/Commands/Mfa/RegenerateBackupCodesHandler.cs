namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa;

using MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands;
using MazyPlatform.Service.User.Authentication.Domain.Services;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Abstractions;
using MazyPlatform.SharedKernel.Domain.Results;

using Microsoft.Extensions.Logging;

internal sealed partial class RegenerateBackupCodesHandler(
    RegenerateBackupCodesService regenerateBackupCodesService,
    IUnitOfWork unitOfWork,
    ILogger<RegenerateBackupCodesHandler> logger) : ICommandHandler<RegenerateBackupCodesCommand, IReadOnlyCollection<BackupCode>>
{
    public async Task<Result<IReadOnlyCollection<BackupCode>>> HandleAsync(RegenerateBackupCodesCommand command, CancellationToken cancellationToken = default)
    {
        var mfaSessionId = Guid.Parse(command.MfaSessionId);
        var userAccountId = Guid.Parse(command.UserAccountId);

        var regenerateR = await regenerateBackupCodesService.ExecuteAsync(mfaSessionId, userAccountId, cancellationToken);
        if (regenerateR.IsFailure)
        {
            RegenerateBackupCodesFailed(mfaSessionId, regenerateR.Errors);
            return regenerateR.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        BackupCodesRegenerated(mfaSessionId);

        return regenerateR;
    }

    #region Logging
    [LoggerMessage(1, LogLevel.Warning, "Ошибка генерации резервных кодов для MFA сессии '{MfaSessionId}': {Errors}")]
    private partial void RegenerateBackupCodesFailed(Guid mfaSessionId, object errors);

    [LoggerMessage(2, LogLevel.Information, "Резервные коды успешно перегенерированы для MFA сессии '{MfaSessionId}'.")]
    private partial void BackupCodesRegenerated(Guid mfaSessionId);
    #endregion
}
