namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Commands.Auth;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class ResendConfirmationCodeValidator : AbstractValidator<ResendConfirmationCodeCommand>
{
    public ResendConfirmationCodeValidator()
    {
        RuleFor(x => x.Email)
             .NotEmpty().WithErrorCode(ErrorCodes.Auth.Email.Required).WithMessage("Email обязателен для заполнения.")
             .EmailAddress().WithErrorCode(ErrorCodes.Auth.Email.InvalidFormat).WithMessage("Email должен быть в правильном формате.");
    }
}
