namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Mfa.Email;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.OtpId)
            .RequiredGuid(nameof(ConfirmEmailCommand.OtpId));

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("OTP код обязателен для заполнения.")
            .Length(6).WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage("OTP код должен быть длиной 6 символов.")
            .Matches("^[0-9]+$").WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage("OTP код должен содержать только цифры.");
    }
}
