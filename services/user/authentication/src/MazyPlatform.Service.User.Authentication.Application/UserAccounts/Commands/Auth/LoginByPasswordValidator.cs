namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class LoginByPasswordValidator : AbstractValidator<LoginByPasswordCommand>
{
    public LoginByPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Email.Required).WithMessage("Email обязателен для заполнения.")
            .EmailAddress().WithErrorCode(ErrorCodes.Auth.Email.InvalidFormat).WithMessage("Email должен быть в правильном формате.");

        RuleFor(x => x.Password)
            .RequiredStrongPassword(nameof(LoginByPasswordCommand.Password));

        RuleFor(x => x.MfaSessionId)
            .OptionalGuid(nameof(LoginByPasswordCommand.MfaSessionId));
    }
}
