namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class RegenerateBackupCodesValidator : AbstractValidator<RegenerateBackupCodesCommand>
{
    public RegenerateBackupCodesValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(RegenerateBackupCodesCommand.MfaSessionId));

        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(RegenerateBackupCodesCommand.UserAccountId));
    }
}
