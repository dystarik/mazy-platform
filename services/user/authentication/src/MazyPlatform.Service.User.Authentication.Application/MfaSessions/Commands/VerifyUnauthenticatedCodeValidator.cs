namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class VerifyUnauthenticatedCodeValidator : AbstractValidator<VerifyUnauthenticatedCodeCommand>
{
    public VerifyUnauthenticatedCodeValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(VerifyUnauthenticatedCodeCommand.MfaSessionId));

        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage("Code обязателен для заполнения.");
    }
}
