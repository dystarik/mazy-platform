namespace MazyPlatform.Service.Scenario.Repository.Application.Decorators;

using FluentValidation;

using MazyPlatform.SharedKernel.Application.Abstractions.Commands;
using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

using Microsoft.Extensions.Logging;

internal sealed partial class ValidationCommandDecoratorVoid<TCommand>(
    IValidator<TCommand> validator,
    ICommandHandler<TCommand> inner,
    ILogger<ValidationCommandDecoratorVoid<TCommand>> logger) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
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
