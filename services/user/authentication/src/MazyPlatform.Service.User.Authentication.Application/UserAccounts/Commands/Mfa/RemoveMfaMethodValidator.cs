namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class RemoveMfaMethodValidator : AbstractValidator<RemoveMfaMethodCommand>
{
    public RemoveMfaMethodValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(RemoveMfaMethodCommand.MfaSessionId));

        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(RemoveMfaMethodCommand.UserAccountId));
    }
}
