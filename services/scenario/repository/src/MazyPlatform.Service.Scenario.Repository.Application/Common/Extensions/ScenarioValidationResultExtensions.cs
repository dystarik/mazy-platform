namespace MazyPlatform.Service.Scenario.Repository.Application.Common.Extensions;

using System.Text;

using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;
using MazyPlatform.SharedKernel.Domain.Results.Errors;

internal static class ScenarioValidationResultExtensions
{
    public static ErrorCollection ToErrorCollection(this ScenarioValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        var errors = validationResult.Errors
            .Select(MapError)
            .ToArray();

        return new ErrorCollection(errors);
    }

    private static Error MapError(ScenarioValidationError validationError)
    {
        ArgumentNullException.ThrowIfNull(validationError);

        return Error.Validation(validationError.Code, FormatMessage(validationError));
    }

    private static string FormatMessage(ScenarioValidationError validationError)
    {
        var message = new StringBuilder(validationError.Message);

        if (validationError.NodeId is not null)
        {
            message.Append(" [nodeId: ");
            message.Append(validationError.NodeId.Value);
            message.Append(']');
        }

        if (!string.IsNullOrWhiteSpace(validationError.Path))
        {
            message.Append(" [path: ");
            message.Append(validationError.Path);
            message.Append(']');
        }

        return message.ToString();
    }
}
