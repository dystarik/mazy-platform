namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class CompleteRegistrationValidator : AbstractValidator<CompleteRegistrationCommand>
{
    public CompleteRegistrationValidator()
    {
        RuleFor(x => x.OtpId)
            .RequiredGuid(nameof(CompleteRegistrationCommand.OtpId));

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("OTP код обязателен для заполнения.")
            .Length(6).WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage("OTP код должен быть длиной 6 символов.");
    }
}
