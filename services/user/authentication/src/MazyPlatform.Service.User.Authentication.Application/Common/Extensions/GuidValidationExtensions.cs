namespace MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal static class GuidValidationExtensions
{
    public static IRuleBuilderOptions<T, string> RequiredGuid<T>(this IRuleBuilder<T, string> ruleBuilder, string fieldName)
    {
        return ruleBuilder
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Required).WithMessage($"{fieldName} обязателен для заполнения.")
            .Must(static x => Guid.TryParse(x, out _)).WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage($"{fieldName} должен быть валидным GUID.");
    }

    public static IRuleBuilderOptions<T, string?> OptionalGuid<T>(this IRuleBuilder<T, string?> ruleBuilder, string fieldName)
    {
        return ruleBuilder
            .Must(static x => string.IsNullOrWhiteSpace(x) || Guid.TryParse(x, out _))
            .WithErrorCode(ErrorCodes.Auth.Validation.Invalid)
            .WithMessage($"{fieldName} должен быть валидным GUID.");
    }
}
