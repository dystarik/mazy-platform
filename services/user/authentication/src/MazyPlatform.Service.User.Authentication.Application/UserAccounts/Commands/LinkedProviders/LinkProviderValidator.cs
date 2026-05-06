namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.LinkedProviders;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class LinkProviderValidator : AbstractValidator<LinkProviderCommand>
{
    public LinkProviderValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(LinkProviderCommand.UserAccountId));

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("Код авторизации обязателен для заполнения.");
    }
}
