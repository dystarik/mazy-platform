namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class LoginByExternalProviderValidator : AbstractValidator<LoginByExternalProviderCommand>
{
    public LoginByExternalProviderValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("Код авторизации обязателен для заполнения.");
    }
}
