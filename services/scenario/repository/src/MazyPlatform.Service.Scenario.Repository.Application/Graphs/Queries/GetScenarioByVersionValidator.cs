namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using FluentValidation;

internal sealed class GetScenarioByVersionValidator : AbstractValidator<GetScenarioByVersionQuery>
{
    public GetScenarioByVersionValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("ProjectId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ProjectId имеет неверный формат.");

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("Version должен быть больше нуля.");
    }
}
