namespace MazyPlatform.Service.User.Authentication.Application.Decorators;

using FluentValidation;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class ValidationCommandDecorator<TCommand, TResponse>(
    IValidator<TCommand> validator,
    ICommandHandler<TCommand, TResponse> inner,
    ILogger<ValidationCommandDecorator<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var errors = await ValidationExecutionHelper.ValidateAsync(command, validator, cancellationToken);
        if (errors is null)
            return await inner.HandleAsync(command, cancellationToken);

        FailedValidation(typeof(TCommand).Name, errors);
        return errors;
    }

    [LoggerMessage(1, LogLevel.Warning, "Валидация команды {CommandName} завершилась с ошибками: {Errors}")]
    private partial void FailedValidation(string commandName, ErrorCollection errors);
}
