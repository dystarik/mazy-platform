namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class AddEmailValidator : AbstractValidator<AddEmailCommand>
{
    public AddEmailValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(AddEmailCommand.UserAccountId));
    }
}
