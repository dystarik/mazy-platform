namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using FluentValidation;

internal sealed class GetReleasedScenarioValidator : AbstractValidator<GetReleasedScenarioQuery>
{
    public GetReleasedScenarioValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("ProjectId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ProjectId имеет неверный формат.");
    }
}
