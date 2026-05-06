namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using FluentValidation;

internal sealed class ValidateProjectForBotValidator : AbstractValidator<ValidateProjectForBotQuery>
{
    public ValidateProjectForBotValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required);

        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required);

        RuleFor(x => x.ScenarioVersion)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.Validation.Invalid);
    }
}
