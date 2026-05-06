namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Totp;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class ConfirmTotpValidator : AbstractValidator<ConfirmTotpCommand>
{
    public ConfirmTotpValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(ConfirmTotpCommand.UserAccountId));

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("Code обязателен для заполнения.")
            .Length(6).WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage("Code должен быть длиной 6 символов.")
            .Matches("^[0-9]+$").WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage("Code должен содержать только цифры.");
    }
}
