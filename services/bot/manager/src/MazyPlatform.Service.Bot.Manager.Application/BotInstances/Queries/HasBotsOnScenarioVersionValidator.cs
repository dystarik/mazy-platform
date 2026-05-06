namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using FluentValidation;

internal sealed class HasBotsOnScenarioVersionValidator : AbstractValidator<HasBotsOnScenarioVersionQuery>
{
    public HasBotsOnScenarioVersionValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("ProjectId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ProjectId имеет неверный формат.");

        RuleFor(x => x.ScenarioVersion)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ScenarioVersion должен быть положительным числом.");
    }
}
