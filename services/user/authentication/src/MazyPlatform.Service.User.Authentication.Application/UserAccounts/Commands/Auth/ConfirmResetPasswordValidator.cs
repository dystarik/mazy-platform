namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class ConfirmResetPasswordValidator : AbstractValidator<ConfirmResetPasswordCommand>
{
    public ConfirmResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .RequiredStrongPassword(nameof(ConfirmResetPasswordCommand.NewPassword));

        RuleFor(x => x.OtpId)
            .OptionalGuid(nameof(ConfirmResetPasswordCommand.OtpId));

        RuleFor(x => x.MfaSessionId)
            .OptionalGuid(nameof(ConfirmResetPasswordCommand.MfaSessionId));

        RuleFor(x => x)
            .Must(HasValidChallenge)
            .WithErrorCode(ErrorCodes.Auth.Validation.Invalid)
            .WithMessage("Должны быть переданы либо OtpId и OtpCode, либо MfaSessionId.");
    }

    private static bool HasValidChallenge(ConfirmResetPasswordCommand command)
    {
        var hasOtp = !string.IsNullOrWhiteSpace(command.OtpId) && !string.IsNullOrWhiteSpace(command.OtpCode);
        var hasMfaSession = !string.IsNullOrWhiteSpace(command.MfaSessionId);

        return hasOtp ^ hasMfaSession;
    }
}
