namespace MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

internal static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> RequiredStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder, string fieldName)
    {
        return ruleBuilder
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Password.Required).WithMessage($"{fieldName} обязателен для заполнения.")
            .MinimumLength(Password.MinLength).WithErrorCode(ErrorCodes.Auth.Password.TooShort).WithMessage("Неверный формат пароля.")
            .MaximumLength(Password.MaxLength).WithErrorCode(ErrorCodes.Auth.Password.TooLong).WithMessage("Неверный формат пароля.")
            .Matches("[A-Z]").WithErrorCode(ErrorCodes.Auth.Password.NoUpperCase).WithMessage("Неверный формат пароля.")
            .Matches("[a-z]").WithErrorCode(ErrorCodes.Auth.Password.NoLowerCase).WithMessage("Неверный формат пароля.")
            .Matches("[0-9]").WithErrorCode(ErrorCodes.Auth.Password.NoDigit).WithMessage("Неверный формат пароля.")
            .Matches("[^a-zA-Z0-9]").WithErrorCode(ErrorCodes.Auth.Password.NoSpecialChar).WithMessage("Неверный формат пароля.");
    }

    public static IRuleBuilderOptions<T, string> RequiredPassword<T>(this IRuleBuilder<T, string> ruleBuilder, string fieldName)
    {
        return ruleBuilder
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Password.Required).WithMessage($"{fieldName} обязателен для заполнения.");
    }
}
