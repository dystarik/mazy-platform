namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(ChangePasswordCommand.UserAccountId));

        RuleFor(x => x.CurrentPassword)
            .RequiredPassword(nameof(ChangePasswordCommand.CurrentPassword));

        RuleFor(x => x.NewPassword)
            .RequiredStrongPassword(nameof(ChangePasswordCommand.NewPassword));

        RuleFor(x => x.MfaSessionId)
            .OptionalGuid(nameof(ChangePasswordCommand.MfaSessionId));
    }
}
