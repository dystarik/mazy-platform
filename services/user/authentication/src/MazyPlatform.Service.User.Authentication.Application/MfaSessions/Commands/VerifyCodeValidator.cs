namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal class VerifyCodeValidator : AbstractValidator<VerifyCodeCommand>
{
    public VerifyCodeValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(VerifyCodeCommand.MfaSessionId));

        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(VerifyCodeCommand.UserAccountId));

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("Code обязателен для заполнения.");
    }
}
