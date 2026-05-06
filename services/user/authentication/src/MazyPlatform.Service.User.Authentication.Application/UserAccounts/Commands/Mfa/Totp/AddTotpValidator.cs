namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class AddTotpValidator : AbstractValidator<AddTotpCommand>
{
    public AddTotpValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(AddTotpCommand.UserAccountId));
    }
}
