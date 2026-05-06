namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.LinkedProviders;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class UnlinkProviderValidator : AbstractValidator<UnlinkProviderCommand>
{
    public UnlinkProviderValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(UnlinkProviderCommand.UserAccountId));
    }
}
