namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class SetPasswordValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(SetPasswordCommand.UserAccountId));

        RuleFor(x => x.NewPassword)
            .RequiredStrongPassword(nameof(SetPasswordCommand.NewPassword));

        RuleFor(x => x.MfaSessionId)
            .OptionalGuid(nameof(SetPasswordCommand.MfaSessionId));
    }
}
