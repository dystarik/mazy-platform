namespace MazyPlatform.Service.Scenario.Repository.Application.Decorators;

using FluentValidation;

using MazyPlatform.Service.Scenario.Repository.Application.Common.Extensions;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

internal static class ValidationExecutionHelper
{
    public static async Task<ErrorCollection?> ValidateAsync<TRequest>(TRequest request, IValidator<TRequest> validator, CancellationToken cancellationToken)
    {
        var validationResult = (await validator.ValidateAsync(request, cancellationToken)).ToResult();
        if (validationResult.IsSuccess)
            return null;

        return validationResult.Errors;
    }
}
