namespace MazyPlatform.Service.Bot.Manager.Application.Common.Extensions;

using FluentValidation.Results;

using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

internal static class FluentValidationResultExtensions
{
    public static Result ToResult(this ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return Result.Success();

        var errors = validationResult.Errors
            .Where(x => x is not null)
            .Select(x => Error.Validation(x.ErrorCode, x.ErrorMessage)).ToArray();

        return errors.Length is 0 ? Result.Success() : Result.Failure(new ErrorCollection(errors));
    }
}
