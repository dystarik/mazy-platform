namespace MazyPlatform.Service.User.Authentication.Application.Decorators;

using FluentValidation;

using MazyPlatform.SharedKernel.Application.Abstractions.Queries;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class ValidationQueryDecorator<TQuery, TResponse>(
    IValidator<TQuery> validator,
    IQueryHandler<TQuery, TResponse> inner,
    ILogger<ValidationQueryDecorator<TQuery, TResponse>> logger) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var errors = await ValidationExecutionHelper.ValidateAsync(query, validator, cancellationToken);
        if (errors is null)
            return await inner.HandleAsync(query, cancellationToken);

        FailedValidation(typeof(TQuery).Name, errors);
        return errors;
    }

    [LoggerMessage(1, LogLevel.Warning, "Валидация запроса {QueryName} завершилась с ошибками: {Errors}")]
    private partial void FailedValidation(string queryName, ErrorCollection errors);
}
